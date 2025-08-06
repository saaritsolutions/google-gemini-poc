#!/bin/bash

# GitHub Actions Workflow Validation Script
echo "🔍 Validating GitHub Actions Workflow..."

# Check if workflow file exists
if [ ! -f ".github/workflows/auto-documentation.yml" ]; then
    echo "❌ Workflow file not found!"
    exit 1
fi

echo "✅ Workflow file exists"

# Check YAML syntax
if command -v yamllint &> /dev/null; then
    yamllint .github/workflows/auto-documentation.yml
    if [ $? -eq 0 ]; then
        echo "✅ YAML syntax is valid"
    else
        echo "❌ YAML syntax errors found"
        exit 1
    fi
else
    echo "⚠️  yamllint not found, skipping syntax check"
fi

# Validate GitHub Actions syntax using act (if available)
if command -v act &> /dev/null; then
    echo "🔍 Validating with act..."
    act --list
    if [ $? -eq 0 ]; then
        echo "✅ GitHub Actions syntax is valid"
    else
        echo "❌ GitHub Actions syntax errors found"
    fi
else
    echo "⚠️  act not found, skipping GitHub Actions validation"
    echo "💡 Install act: brew install act (macOS) or visit https://github.com/nektos/act"
fi

# Check required secrets
echo "🔐 Required secrets for this workflow:"
echo "  - GEMINI_API_KEY: Your Google Gemini API key"
echo "  - GITHUB_TOKEN: Automatically provided by GitHub"

# Validate workflow triggers
echo "🎯 Workflow triggers:"
echo "  - Pull requests (opened, synchronize, ready_for_review)"
echo "  - Target branches: main, develop"

# Check file patterns
echo "📁 Files monitored for changes:"
echo "  - *.cs, *.js, *.ts, *.py, *.java, *.cpp, *.h"
echo "  - Excludes: bin/, obj/, node_modules/, *.min.js"

echo "✅ Workflow validation complete!"
