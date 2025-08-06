#!/bin/bash

# Test Documentation Generation Locally
echo "🧪 Testing Documentation Generation Locally..."

# Set environment variables for testing
export GEMINI_API_KEY="AIzaSyDzS4J6IgKLnjIxDvKelPOafShR3F0JNEw"
export GITHUB_TOKEN="dummy-token-for-testing"

# Test with mock parameters
echo "🔧 Testing documentation mode with mock parameters..."

dotnet run -- \
  --mode documentation \
  --repo "saaritsolutions/poc" \
  --pr-number 1

if [ $? -eq 0 ]; then
    echo "✅ Documentation mode executed successfully"
else
    echo "❌ Documentation mode failed"
    echo "💡 Check that your Gemini API key is valid and the application builds correctly"
fi
