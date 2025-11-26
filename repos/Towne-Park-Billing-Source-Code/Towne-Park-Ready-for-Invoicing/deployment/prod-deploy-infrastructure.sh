echo started deploying resources

az deployment group create \
 --resource-group RSS-PROD \
 --template-file ../infrastructure/main.bicep \
 --parameters ../infrastructure/params/prod.bicepparam \

echo deployment complete.