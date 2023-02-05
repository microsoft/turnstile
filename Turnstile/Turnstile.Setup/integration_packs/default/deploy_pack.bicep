param deploymentName string

param location string = resourceGroup().location

// For subscribing to the event grid topic...

param eventGridConnectionId string
param eventGridTopicId string
param managedIdId string

var packName = 'default'

module onLowSeatWarningLevelReached './on_low_seat_warning_level_reached.bicep' = {
  name: '${packName}-pack-deploy-on-low-seat-warning-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onNoSeatsAvailable './on_no_seats_available.bicep' = {
  name: '${packName}-pack-deploy-on-no-seats-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onSeatProvided './on_seat_provided.bicep' = {
  name: '${packName}-pack-deploy-on-seat-provided-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onSeatRedeemed './on_seat_redeemed.bicep' = {
  name: '${packName}-pack-deploy-on-seat-redeemed-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onSeatReserved './on_seat_reserved.bicep' = {
  name: '${packName}-pack-deploy-on-seat-reserved-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onSubscriptionCreated './on_subscription_created.bicep' = {
  name: '${packName}-pack-deploy-on-sub-created-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

module onSubscriptionUpdated './on_subscription_updated.bicep' = {
  name: '${packName}-pack-deploy-on-sub-updated-${deploymentName}'
  params: {
    deploymentName: deploymentName
    location: location
    eventGridConnectionId: eventGridConnectionId
    eventGridTopicId: eventGridTopicId
    managedIdId: managedIdId
  }
}

