# ==========================================
# SS-ProfileService Scaffolding Script (PowerShell)
# ==========================================

# 1. Create directory structure
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Domain/Common
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Domain/Entities
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Features/Profiles/CreateProfile
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Features/Profiles/GetProfileById
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Features/Profiles/UpdateProfile
New-Item -ItemType Directory -Force -Path src/SS.ProfileService.API/Infrastructure/Data
New-Item -ItemType Directory -Force -Path test/SS.ProfileService.Tests

Write-Host "Directory structure scaffolded successfully!" -ForegroundColor Green
