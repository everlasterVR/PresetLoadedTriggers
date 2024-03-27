#!/bin/bash

files=$(grep -o '<Compile Include="[^"]*"' PresetLoadedTriggers.csproj | sed 's/<Compile Include="//; s/"//')
echo "$files" > PresetLoadedTriggers.cslist
