echo started deploying resources

az deployment group create \
 --resource-group RSS-UAT \
 --template-file ../infrastructure/main.bicep \
 --parameters ../infrastructure/params/uat.bicepparam \

echo deployment complete.