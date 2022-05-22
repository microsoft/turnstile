#!/bin/bash


#az account show
#az account set --subscription "SUBS NAME"
#sh deploy.sh turnstile-turntest30 turn-web-turntest30

resource_group_name="$1"
web_app_name="$2"

dotnet publish -c Release -o ./web_topublish ../Turnstile.Web/Turnstile.Web.csproj

cd ./web_topublish
zip -r ../web_topublish.zip . >/dev/null
cd ..

echo "☁️    Publishing web app [$web_app_name]..."

az webapp deployment source config-zip \
    --resource-group "$resource_group_name" \
    --name "$web_app_name" \
    --src "./web_topublish.zip"

echo "🧹   Cleaning up..."

rm -rf ./web_topublish >/dev/null
rm -rf ./web_topublish.zip >/dev/null


echo "➡️   deployment completed."




