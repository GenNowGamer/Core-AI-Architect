import fs from 'node:fs';
import path from 'node:path';
import https from 'node:https';
import { execSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRoot = path.resolve(__dirname, '..');
const artifactsDir = path.join(repoRoot, 'artifacts');

function getGitHubToken() {
  if (process.env.GITHUB_TOKEN) return process.env.GITHUB_TOKEN.trim();
  try {
    const creds = execSync('git credential fill', {
      input: 'protocol=https\nhost=github.com\n\n',
      stdio: ['pipe', 'pipe', 'ignore'],
    }).toString();
    const line = creds.split('\n').find((l) => l.startsWith('password='));
    if (line) return line.substring('password='.length).trim();
  } catch (err) {
    // ignore
  }
  return null;
}

function getRepoCoordinates() {
  try {
    const remoteUrl = execSync('git remote get-url origin', { cwd: repoRoot }).toString().trim();
    const match = remoteUrl.match(/github\.com[:\/]([^\/]+)\/([^\/\.]+)(?:\.git)?$/i);
    if (match) {
      return { owner: match[1], repo: match[2] };
    }
  } catch {}
  return { owner: 'GenNowGamer', repo: 'Core-AI-Architect' };
}

function getCurrentVersion() {
  const propsPath = path.join(repoRoot, 'Directory.Build.props');
  if (fs.existsSync(propsPath)) {
    const content = fs.readFileSync(propsPath, 'utf8');
    const match = content.match(/<Version>([^<]+)<\/Version>/i);
    if (match) return match[1].trim();
  }
  return '1.1.0';
}

function requestGithub(pathname, method = 'GET', data = null, headers = {}) {
  const token = getGitHubToken();
  if (!token) throw new Error('No GitHub credential or GITHUB_TOKEN found.');

  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'api.github.com',
      path: pathname,
      method,
      headers: {
        'User-Agent': 'Core-AI-Architect-Release-Automation',
        'Authorization': `token ${token}`,
        'Accept': 'application/vnd.github.v3+json',
        ...headers,
      },
    };

    const req = https.request(options, (res) => {
      let body = '';
      res.on('data', (d) => (body += d));
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            resolve(JSON.parse(body || '{}'));
          } catch {
            resolve(body);
          }
        } else {
          reject(new Error(`GitHub API error ${res.statusCode}: ${body}`));
        }
      });
    });

    req.on('error', reject);
    if (data) req.write(data);
    req.end();
  });
}

function uploadAsset(uploadUrlTemplate, filePath, fileName) {
  const token = getGitHubToken();
  const fileBuffer = fs.readFileSync(filePath);
  const uploadUrl = uploadUrlTemplate.replace(/\{[^\}]+\}/g, '') + `?name=${encodeURIComponent(fileName)}`;
  const urlObj = new URL(uploadUrl);

  console.log(`Uploading ${fileName} (${(fileBuffer.length / (1024 * 1024)).toFixed(2)} MB)...`);

  return new Promise((resolve, reject) => {
    const req = https.request({
      hostname: urlObj.hostname,
      path: urlObj.pathname + urlObj.search,
      method: 'POST',
      headers: {
        'User-Agent': 'Core-AI-Architect-Release-Automation',
        'Authorization': `token ${token}`,
        'Content-Type': 'application/octet-stream',
        'Content-Length': fileBuffer.length,
      },
    }, (res) => {
      let body = '';
      res.on('data', (d) => (body += d));
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try {
            resolve(JSON.parse(body || '{}'));
          } catch {
            resolve(body);
          }
        } else {
          reject(new Error(`Asset upload error ${res.statusCode}: ${body}`));
        }
      });
    });

    req.on('error', reject);
    req.write(fileBuffer);
    req.end();
  });
}

async function purgePriorReleasesAndTags(repoOwner, repoName, currentTagName) {
  console.log('\nChecking for previous releases/tags to enforce single-release policy...');
  try {
    const releases = await requestGithub(`/repos/${repoOwner}/${repoName}/releases?per_page=100`);
    if (Array.isArray(releases)) {
      for (const rel of releases) {
        if (rel.tag_name !== currentTagName) {
          console.log(`Deleting previous release: ${rel.tag_name} (id: ${rel.id})...`);
          try {
            await requestGithub(`/repos/${repoOwner}/${repoName}/releases/${rel.id}`, 'DELETE');
          } catch (e) {
            console.warn(`Could not delete release ${rel.id}: ${e.message}`);
          }
        }
      }
    }
  } catch (err) {
    console.warn(`Note on prior releases check: ${err.message}`);
  }

  try {
    const tags = await requestGithub(`/repos/${repoOwner}/${repoName}/git/matching-refs/tags`);
    if (Array.isArray(tags)) {
      for (const refItem of tags) {
        const tagName = refItem.ref.replace('refs/tags/', '');
        if (tagName !== currentTagName) {
          console.log(`Deleting prior git tag: ${tagName}...`);
          try {
            await requestGithub(`/repos/${repoOwner}/${repoName}/git/refs/tags/${encodeURIComponent(tagName)}`, 'DELETE');
          } catch (e) {
            console.warn(`Could not delete tag ref ${tagName}: ${e.message}`);
          }
        }
      }
    }
  } catch (err) {
    console.warn(`Note on prior tags check: ${err.message}`);
  }
}

async function main() {
  const { owner, repo } = getRepoCoordinates();
  const version = getCurrentVersion();
  const tagName = `v${version}`;
  const releaseTitle = `Core AI Architect v${version}`;

  console.log('======================================================');
  console.log(`Publishing GitHub Release for ${owner}/${repo}`);
  console.log(`Target Version: ${tagName}`);
  console.log('======================================================\n');

  // 1. Ensure fresh application and setup MSI are built
  const msiPath = path.join(artifactsDir, 'CoreAiArchitect-Setup.msi');
  if (!fs.existsSync(msiPath)) {
    console.log('Setup MSI not found in artifacts. Compiling now...');
    execSync('powershell -ExecutionPolicy Bypass -File tools/Build-Installer.ps1', {
      cwd: repoRoot,
      stdio: 'inherit',
    });
  }

  // 2. Enforce single release policy
  await purgePriorReleasesAndTags(owner, repo, tagName);

  // 3. Create or update the release
  console.log(`\nCreating GitHub release for ${tagName}...`);
  const releaseNotes = `## Core AI Architect v${version}

### Windows Desktop App & AI Blueprint Authoring Tool
**Core AI Architect** is an authoring tool designed to convert your plain-English software concepts into a complete, validated **App Blueprint** (SSOT) tailored for autonomous AI coding agents (Claude Code, Cursor, Gemini CLI, Copilot).

---

### Highlights & Key Capabilities

- **Single Windows Installer (\`CoreAiArchitect-Setup.msi\`):**
  - Fully self-contained installation (no .NET 8 SDK or desktop runtime downloads required).
  - Installs to \`Program Files\`, creates Start Menu and Desktop shortcuts with high-resolution icons.
  - Full Windows Add/Remove Programs support with clean uninstallation.
- **Modern Fluent Dark Theme:**
  - High-contrast charcoal and slate design with dynamic brush resources.
  - Styled custom \`ComboBox\`, list boxes, tabs, text blocks, and scrollbars.
- **Guided Ad-Lib & Needs Specification:**
  - Plain-language prompts (Target Audience, Solved Problems, Core Capabilities).
  - Multi-device, user accounts, and notifications requirement questionnaires.
- **Overhauled Starter Templates:**
  - **Start Blank:** Empty canvas for full flexibility.
  - **Master-Detail / Catalog:** Filterable search and item list with dedicated detail/edit views.
  - **Dashboard & Analytics:** Metric cards, time filters, recent event tables, and reporting.
  - **File / Batch Processor:** Queue processing, destination paths, preset selections, and status logs.
  - **Simple Form & List + History:** Everyday forms and persistence foundations.
- **Live Visual Wireframe Canvas:**
  - Interactive mockup canvas showing platform-aware window/device chrome with real-time control updates and ▲ / ▼ visual reordering.
- **Undo / Redo Architecture:**
  - Full snapshot-based undo (\`Ctrl+Z\`) and redo (\`Ctrl+Y\`) stack across all screens and controls.
- **In-App Artifact Inspection:**
  - Live preview tabs for **App Story**, **Blueprint JSON**, **AGENTS.md**, **Specifications**, and phased **Tasks**.
- **Automated AI Handoff & Git Workspace Setup:**
  - One-click **"Copy Kickoff Prompt"** button formatted for AI coding agent kickoff.
  - Preconfigured agent context files (\`.cursorrules\`, \`CLAUDE.md\`, \`.gemini/config.json\`, \`.gitignore\`).
  - Automatic git repository initialization and commit upon export.

---

### Installation Instructions

1. Download **\`CoreAiArchitect-Setup.msi\`** below.
2. Double-click the \`.msi\` installer to install Core AI Architect to your PC.
3. Launch the app directly from your Desktop or Start Menu.
`;

  const payload = JSON.stringify({
    tag_name: tagName,
    target_commitish: 'main',
    name: releaseTitle,
    body: releaseNotes,
    draft: false,
    prerelease: false,
  });

  let release;
  try {
    release = await requestGithub(`/repos/${owner}/${repo}/releases`, 'POST', payload, {
      'Content-Type': 'application/json',
    });
  } catch (err) {
    if (err.message.includes('already_exists')) {
      console.log(`Release ${tagName} already exists. Fetching existing release...`);
      release = await requestGithub(`/repos/${owner}/${repo}/releases/tags/${tagName}`);
      try {
        release = await requestGithub(`/repos/${owner}/${repo}/releases/${release.id}`, 'PATCH', payload, {
          'Content-Type': 'application/json',
        });
      } catch (patchErr) {
        console.warn(`Could not update release payload: ${patchErr.message}`);
      }
    } else {
      throw err;
    }
  }

  console.log(`Release created/ready: ${release.html_url}`);

  // 4. Delete existing assets on this release before re-uploading
  if (Array.isArray(release.assets) && release.assets.length > 0) {
    console.log(`Cleaning ${release.assets.length} existing asset(s) on release...`);
    for (const asset of release.assets) {
      try {
        await requestGithub(`/repos/${owner}/${repo}/releases/assets/${asset.id}`, 'DELETE');
        console.log(`Deleted existing asset: ${asset.name}`);
      } catch (e) {
        console.warn(`Could not delete asset ${asset.name}: ${e.message}`);
      }
    }
  }

  // 5. Upload Setup MSI
  if (fs.existsSync(msiPath)) {
    await uploadAsset(release.upload_url, msiPath, 'CoreAiArchitect-Setup.msi');
    console.log('Uploaded CoreAiArchitect-Setup.msi successfully.');
  }

  console.log('\n======================================================');
  console.log(`SUCCESS: Core AI Architect v${version} Release Published!`);
  console.log(`URL: ${release.html_url}`);
  console.log('======================================================\n');
}

main().catch((err) => {
  console.error('\nRelease automation failed:', err);
  process.exit(1);
});
