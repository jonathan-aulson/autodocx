param vnetName string
param subnetName string

resource vnet 'Microsoft.Network/virtualNetworks@2023-02-01' existing = {
  name: vnetName
}

resource subnet 'Microsoft.Network/virtualNetworks/subnets@2023-02-01' existing = {
  parent: vnet
  name: subnetName
}

output subnetId string = subnet.id
output vnetId string = vnet.id
