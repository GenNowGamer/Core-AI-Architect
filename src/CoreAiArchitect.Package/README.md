# MSIX packaging notes
#
# This folder contains a Windows Application Packaging Project for Visual Studio.
# It is intentionally not added to CoreAiArchitect.sln so `dotnet build` remains
# portable without Desktop Bridge / MSIX tooling installed.
#
# To package:
# 1. Install MSIX Packaging Tools in Visual Studio
# 2. Open CoreAiArchitect.Package.wapproj (File > Open > Project/Solution)
#    or Add Existing Project to the solution in Visual Studio
# 3. Ensure CoreAiArchitect.App is referenced under Applications
# 4. Build/Publish for x64 sideloading
#
# Reference:
# https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-packaging-dot-net
