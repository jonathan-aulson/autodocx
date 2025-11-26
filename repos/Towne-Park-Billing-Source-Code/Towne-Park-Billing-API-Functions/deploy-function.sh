#!/bin/bash
source deploy.env

# CONFIGURATION
RESOURCE_GROUP="${RESOURCE_GROUP}"
FUNCTION_APP_NAME="${FUNCTION_APP_NAME}"
LOCATION="${LOCATION}"
ZIP_PATH="${ZIP_PATH}"
DEPLOYMENT_NAME="${DEPLOYMENT_NAME}"
BICEP_FILE="${BICEP_FILE}"
PARAM_FILE="${PARAM_FILE}"

# CHECK IF FUNCTION APP EXISTS
echo "🔍 Checking if Function App '$FUNCTION_APP_NAME' exists..."
az functionapp show --name "$FUNCTION_APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null

if [ $? -eq 0 ]; then
  echo "✅ Function App exists. Will update its content."
else
  echo "⚙️ Function App does not exist. Deploying infrastructure..."
  az deployment group create \
    --name "$DEPLOYMENT_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_FILE" \
    --parameters @"$PARAM_FILE"
fi

# DEPLOY ZIP PACKAGE
if [ -f "$ZIP_PATH" ]; then
  echo "📦 Deploying function package from '$ZIP_PATH'..."
  az functionapp deployment source config-zip \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --src "$ZIP_PATH"
  echo "✅ Deployment complete!"

  echo "🌐 Configuring CORS to allow Azure Portal testing..."
  az functionapp cors add \
    --name "$FUNCTION_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --allowed-origins https://portal.azure.com
  echo "✅ CORS configuration complete!"
else
  echo "❌ ZIP file not found at '$ZIP_PATH'. Make sure to run 'package.sh' first."
  exit 1
fi
