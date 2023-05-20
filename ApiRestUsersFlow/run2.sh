#!/bin/bash

# Set the project name
project="ApiUsersFlow"

# Check if pv is installed
if ! command -v pv &> /dev/null
then
    echo "pv is not installed. Installing now..."
    apt-get update
    apt-get install -y pv
fi

# Build the project
echo "Building the project ..."
dotnet build "$project.csproj" | pv -p --timer --rate --bytes >/dev/null
echo "Project successfully built!"
