#!/bin/bash

api_url=$1
api_key=$2

# All of the resources that this test needs to run are located in this folder. When called from ../e2e.sh,
# this script inherits ../e2e.sh's pwd so we can't really use "./" to refer to this folder's path. Instead,
# we use this super-helpful bit of shell to get this folder's path.

here=$(cd "$(dirname "$(readlink -f "$0")")" >/dev/null 2>&1 && pwd)

subscription_json=$(cat "$here/subscription.json")
subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")

echo "Running [can redeem reserved seat] test..."

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
    # Let's reserve a seat by email...

    seat_id=$(cat /proc/sys/kernel/random/uuid)
    reserve_url="$api_url/api/saas/subscriptions/$subscription_id/seats/$seat_id/reserve"
    email_reservation_json=$(cat "$here/email_reservation.json")
    reservation_email=$(echo "$email_reservation_json" | jq -r ".email")

    echo "POSTing reservation request for email [$reservation_email] to [$reserve_url]..."

    reserve_email_response=$(curl \
        -X POST \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        -d "$email_reservation_json" \
        "$reserve_url")

    reserved_seat_id=$(echo "$reserve_email_response" | jq -r ".seat_id")
    reserved_in_subscription_id=$(echo "$reserve_email_response" | jq -r ".subscription_id")
    reserved_for_email=$(echo "$reserve_email_response" | jq -r ".reservation.email")

    if [[ "$seat_id" == "$reserved_seat_id" && "$subscription_id" == "$reserved_in_subscription_id" && "$reservation_email" == "$reserved_for_email" ]]; then
        # Alright, the seat was successfully reserved. Now, let's see if we can redeem it...

        redeem_url="$api_url/api/saas/subscription/$subscription_id/seats/$seat_id/redeem"
        redeem_json=$(cat "$here/redeem_user.json")
        redeem_tid=$(echo "$redeem_json" | jq -r ".tenant_id")
        redeem_uid=$(echo "$redeem_json" | jq -r ".user_id")

        echo "POSTing reservation redemption request for [$reservation_email] to [$redeem_url]..."

        redeem_seat_response=$(curl \
            -X POST \
            -H "Content-Type: application/json" \
            -H "x-functions-key: $api_key" \
            -d "$redeem_json" \
            "$redeem_url")

        redeemed_seat_id=$(echo "$redeem_seat_response" | jq -r ".seat_id")
        redeemed_in_subscription_id=$(echo "$redeem_seat_response" | jq -r ".subscription_id")
        redeemed_for_tid=$(echo "$redeem_seat_response" | jq -r ".occupant.tenant_id")
        redeemed_for_uid=$(echo "$redeem_seat_response" | jq -r ".occupant_user_id")

        if [[ "$seat_id" == "$reserved_seat_id" && "$subscription_id" == "$reserved_in_subscription_id" && "$redeem_tid" == "$redeemed_for_tid" && "$redeem_uid" == "$redeemed_for_uid" ]]; then
            echo "✔️   Can redeem reserved seat; test passed."
            exit 0
        else
            echo "❌   Can't redeem reserved seat; test failed."
            exit 1
        fi
    else
        echo "❌   Can't reserve seat by email address; test failed."
        echo
        echo "[$reserve_url] responded with..."
        echo "$(echo "$reserve_email_response" | jq ".")"        
        exit 1
    fi
else
    echo "❌   POST subscription [$subscription_id] to [$url] failed with status code: [$post_status_code]."    
    exit 1
fi