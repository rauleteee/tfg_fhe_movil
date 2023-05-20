#!/bin/bash

# Install pv if not already installed
echo "Installing pv to monitor the download rate ..."
sudo apt-get install pv >/dev/null

# Get Ubuntu version
declare repo_version=$(if command -v lsb_release &> /dev/null; then lsb_release -r -s; else grep -oP '(?<=^VERSION_ID=).+' /etc/os-release | tr -d '"'; fi)

# Download Microsoft signing key and repository
echo "Downloading Microsoft signing key and repository..."
wget https://packages.microsoft.com/config/ubuntu/$repo_version/packages-microsoft-prod.deb -O packages-microsoft-prod.deb >/dev/null

# Install Microsoft signing key and repository
dpkg -i packages-microsoft-prod.deb > /dev/null

# Clean up
rm packages-microsoft-prod.deb

# Update packages
echo "Updating packages..."
apt update | pv -p --timer --rate --bytes >/dev/null
echo "Installing dotnet..."

# Install dotnet
apt install apt-transport-https -y >/dev/null
apt update >/dev/null
apt install dotnet-sdk-7.0 -y | pv -p --timer --rate --bytes >/dev/null
apt install dotnet-runtime-7.0 -y >/dev/null

# Check installation
echo -e "\n.NET 7.0 was successfully installed!\n"
dotnet --version