echo started deploying resources

az deployment group create \
 --resource-group RSS-DEV \
 --template-file ../infrastructure/main.bicep \
 --parameters ../infrastructure/params/dev.bicepparam \

echo deployment complete.