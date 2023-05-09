param deploymentName string

param location string = resourceGroup().location

// For subscribing to the event grid topic...

param eventGridConnectionId string
param eventGridConnectionName string
param eventGridTopicId string
param managedIdId string

// Noting. We literally deploy nothing.
