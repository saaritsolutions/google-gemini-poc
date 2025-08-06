#!/bin/bash

echo "🔍 GitHub Actions Workflow Quick Check"
echo "====================================="

echo ""
echo "1. 📁 Workflow File Structure:"
if [ -f ".github/workflows/auto-documentation.yml" ]; then
    echo "   ✅ auto-documentation.yml exists"
    echo "   📏 File size: $(wc -c < .github/workflows/auto-documentation.yml) bytes"
    echo "   📄 Lines: $(wc -l < .github/workflows/auto-documentation.yml) lines"
else
    echo "   ❌ Workflow file missing!"
fi

echo ""
echo "2. 🎯 Workflow Triggers:"
grep -A 5 "^on:" .github/workflows/auto-documentation.yml | sed 's/^/   /'

echo ""
echo "3. 🔐 Required Secrets:"
echo "   📝 GEMINI_API_KEY (you need to set this in GitHub repo settings)"
echo "   🤖 GITHUB_TOKEN (automatically provided)"

echo ""
echo "4. 📦 Dependencies:"
grep -A 10 "uses:" .github/workflows/auto-documentation.yml | grep "uses:" | sed 's/^/   /'

echo ""
echo "5. 🧪 Test Commands:"
echo "   Local validation: ./validate-workflow.sh"
echo "   Documentation test: ./test-documentation.sh" 
echo "   Build test: dotnet build"
echo "   Run test: dotnet run"

echo ""
echo "6. 🌐 GitHub Setup Required:"
echo "   • Repository Settings → Secrets and Variables → Actions"
echo "   • Add secret: GEMINI_API_KEY = your-google-gemini-api-key"
echo "   • Enable Actions in repository settings"
echo "   • Ensure workflow permissions allow content write"

echo ""
echo "7. 📋 Checklist for Production:"
echo "   □ Workflow file exists and is valid"
echo "   □ GEMINI_API_KEY secret configured"
echo "   □ Repository permissions set correctly"
echo "   □ Target branches (main, develop) exist"
echo "   □ .NET 9.0 project builds successfully"
echo "   □ Documentation generation tested locally"

echo ""
echo "✅ Quick check complete!"
