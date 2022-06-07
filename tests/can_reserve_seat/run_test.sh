#!/bin/bash

api_url=$1
api_key=$2

tests_fail=0

# All of the resources that this test needs to run are located in this folder. When called from ../e2e.sh,
# this script inherits ../e2e.sh's pwd so we can't really use "./" to refer to this folder's path. Instead,
# we use this super-helpful bit of shell to get this folder's path.

here=$(cd "$(dirname "$(readlink -f "$0")")" >/dev/null 2>&1 && pwd)

subscription_json=$(cat "$here/subscription.json")
subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")

echo "Running [can reserve seat] test..."

url="$api_url/api/saas/subscriptions/$subscription_id"

echo "POSTing new subscription [$subscription_id] to [$url]..."

post_status_code=$(curl -s \
    -X POST \
    -H "Content-Type: application/json" \
    -H "x-functions-key: $api_key" \
    -d "$subscription_json" \
    -o /dev/null \
    -w "%{http_code}" \
    "$url")

if [[ $post_status_code == "200" ]]; then
    # Let's first try to reserve a seat by email...

    seat_id=$(cat /proc/sys/kernel/random/uuid)
    reserve_url="$api_url/api/saas/subscriptions/$subscription_id/seats/$seat_id/reserve"
    email_reservation_json=$(cat "$here/email_reservation.json")
    reservation_email=$(echo "$email_reservation_json" | jq -r ".email")

    echo "POSTing reservation request for email [$reservation_email] to [$reserve_url]..."

    reserve_email_response=$(curl \
        -X POST \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        -d "$email_reservation_json"
        "$reserve_url")

    reserved_seat_id=$(echo "$reserve_email_response" | jq -r ".seat_id")
    reserved_in_subscription_id=$(echo "$reserve_email_response" | jq -r ".subscription_id")
    reserved_for_email=$(echo "$reserve_email_response" | jq -r ".reservation.email")

    if [[ "$seat_id" == "$reserved_seat_id" && "$subscription_id" == "$reserved_in_subscription_id" && "$reservation_email" == "$reserved_for_email" ]]; then
        echo "✔️   Can reserve seat by email address; test passed."
    else
        echo "❌   Can't reserve seat by email address; test failed."
        echo
        echo "[$reserve_url] responded with..."
        echo "$(echo "$reserve_email_response" | jq ".")"
        tests_fail=1
    fi

    # Now let's try with an OIDC identity...

    seat_id=$(cat /proc/sys/kernel/random/uuid)
    reserve_url="$api_url/api/saas/subscriptions/$subscription_id/seats/$seat_id/reserve"
    oidc_reservation_json=$(cat "$here/oidc_reservation.json")
    reservation_tid=$(echo "$oidc_reservation_json" | jq -r ".tenant_id")
    reservation_uid=$(echo "$oidc_reservation_json" | jq -r ".user_id")

    echo "POSTing reservation request for OIDC tenant/user ID [$reservation_tid/$reservation_uid] to [$reserve_url]..."

    reserve_oidc_response=$(curl \
        -X POST \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        -d "$oidc_reservation_json"
        "$reserve_url")

    reserved_seat_id=$(echo "$reserve_oidc_response" | jq -r ".seat_id")
    reserved_in_subscription_id=$(echo "$reserve_oidc_response" | jq -r ".subscription_id")
    reserved_for_tid=$(echo "$reserve_oidc_response" | jq -r ".reservation.tenant_id")
    reserved_for_uid=$(echo "$reserve_oidc_response" | jq -r ".reservation.user_id")
    
     if [[ "$seat_id" == "$reserved_seat_id" && "$subscription_id" == "$reserved_in_subscription_id" && "$reservation_tid" == "$reserved_for_tid" && "$reservation_uid" == "$reserved_for_uid" ]]; then
        echo "✔️   Can reserve seat by OIDC identity; test passed."
    else
        echo "❌   Can't reserve seat by OIDC identity; test failed."
        echo
        echo "[$reserve_url] responded with..."
        echo "$(echo "$reserve_oidc_response" | jq ".")"
        tests_fail=1
    fi
else
    echo "❌   POST subscription [$subscription_id] to [$url] failed with status code: [$post_status_code]."
    tests_fail=1
fi

exit $tests_fail