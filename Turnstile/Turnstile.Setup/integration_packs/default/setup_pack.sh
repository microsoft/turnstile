#!/bin/bash

SECONDS=0 # Let's time it...

pack_name="default"
pack_version="0.1-experimental"
pack_display_name="Default"

usage() { echo "Usage: $0 <-r topic_region> <-g topic_resource_group> <-n topic_name> <-d deployment_name> <-c aad_client_id> <-t aad_tenant_id> <-s aad_client_secret> [-p pack_resource_group]"; }

check_az() {
    az version >/dev/null

    if [[ $? -ne 0 ]]; then
        echo "❌   Please install the Azure CLI before continuing. See [https://docs.microsoft.com/cli/azure/install-azure-cli] for more information."
        return 1
    else
        echo "✔   Azure CLI installed."
    fi
}

check_topic_exists() {
    topic_resource_group=$1
    topic_name=$2

    topic_id=$(az eventgrid topic show \
        --resource-group "$topic_resource_group" \
        --name "$topic_name" \
        --query "id" \
        --output "tsv")

    if [[ -z $topic_id ]]; then
        echo "❌   Source event grid topic [$topic_name] not found. Unable to deploy integration pack."
        return 1
    else
        echo "✔   Source event grid topic [$topic_name] found."
    fi
}

check_topic_region() {
    region=$1

    region_display_name=$(az account list-locations -o tsv --query "[?name=='$region'].displayName")

    if [[ -z $region_display_name ]]; then
        echo "❌   [$region] is not a valid Azure region. For a full list of Azure regions, run 'az account list-locations -o table'."
        return 1
    else
        echo "✔   [$region] is a valid Azure region ($region_display_name)."
    fi
}

check_deployment_name() {
    name=$1

    if [[ $name =~ ^[a-z0-9]{5,13}$ ]]; then
        echo "✔   [$name] is a valid Turnstile integration pack deployment name."
    else
        echo "❌   [$name] is not a valid Turnstile integration pack deployment name. The name must contain only lowercase letters and numbers and be between 5 and 13 characters in length."
        return 1
    fi
}

splash() {
    echo "$pack_display_name Turnstile Integration Pack | 0.1-experimental"
    echo "https://github.com/microsoft/turnstile"
    echo
    echo "Copyright (c) Microsoft Corporation. All rights reserved."
    echo "Licensed under the MIT License. See LICENSE in project root for more information."
    echo
    echo "🧪   ⚠️   Experimental; don't use in production (yet.)"
    echo
}

# Howdy!

splash

# Make sure all pre-reqs are installed...

echo "Checking setup prerequisites..."

check_az;           [[ $? -ne 0 ]] && prereq_check_failed=1

if [[ -z $prereq_check_failed ]]; then
    echo "✔   All setup prerequisites installed."
else
    echo "❌   Please install all setup prerequisites then try again."
    return 1
fi

# Log in the user if they aren't already...

while [[ -z $current_user_oid ]]; do
    current_user_oid=$(az ad signed-in-user show --query objectId --output tsv 2>/dev/null);
    if [[ -z $current_user_oid ]]; then az login; fi;
done

# Get our parameters...

while getopts "r:g:n:d:c:t:s:p:" opt; do
    case $opt in
        r)
            p_topic_region=$OPTARG
        ;;
        g)
            p_topic_resource_group=$OPTARG
        ;;
        n)
            p_topic_name=$OPTARG
        ;;
        d)
            p_deployment_name=$OPTARG
        ;;
        c)
            p_aad_client_id=$OPTARG
        ;;
        t)
            p_aad_tenant_id=$OPTARG
        ;;
        s)
            p_aad_client_secret=$OPTARG
        ;;
        p)
            p_pack_resource_group=$OPTARG
        ;;
        \?)
            usage
            exit 1
        ;;
    esac
done

echo "Validating script parameters..."

[[ -z $p_topic_region || -z $p_topic_resource_group || -z $p_topic_name || -z $p_deployment_name || -z $aad_client_id || -z $aad_tenant_id || -z $aad_client_secret ]] && { usage; exit 1; }

check_topic_region $p_topic_region;                         [[ $? -ne 0 ]] && param_check_failed=1
check_topic_exists $p_topic_resource_group $p_topic_name;   [[ $? -ne 0 ]] && param_check_failed=1
check_deployment_name $p_deployment_name;                   [[ $? -ne 0 ]] && param_check_failed=1

if [[ -z $param_check_failed ]]; then
    echo "✔   All setup parameters are valid."
else
    echo "❌   Parameter validation failed. Please review and try again."
    return 1
fi

if [[ -z $p_pack_resource_group ]]; then
    resource_group_name="$p_topic_resource_group"
else
    resource_group_name="$p_pack_resource_group"
fi

if [[ $(az group exists --resource-group "$resource_group_name" --output tsv) == false ]]; then
    echo "Creating resource group [$resource_group_name]..."

    az group create --location "$p_topic_region" --name "$resource_group_name"

    if [[ $? -eq 0 ]]; then
        echo "✔   Resource group [$resource_group_name] created."
    else
        echo "❌   Unable to create resource group [$resource_group_name]."
        exit 1
    fi
fi

subscription_id=$(az account show --query id --output tsv);
current_user_tid=$(az account show --query tenantId --output tsv);

echo "🦾   Deploying [$pack_display_name] Turnstile Integration Pack Bicep template to subscription [$subscription_id] resource group [$resource_group_name]..."

az deployment group create \
    --resource-group "$resource_group_name" \
    --name "$pack_deployment_name" \
    --template-file "./pack_deploy.bicep" \
    --parameters \
        eventGridTopicLocation="$p_topic_region" \
        eventGridTopicResourceGroup="$p_topic_resource_group" \
        eventGridTopicName="$p_topic_name" \
        packName="$p_deployment_name" \
        aadClientId="$p_aad_client_id" \
        aadTenantId="$p_aad_tenant_id" \
        aadClientSecret="$p_aad_client_secret"



