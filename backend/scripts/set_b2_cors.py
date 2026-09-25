#!/usr/bin/env python3
"""
One-time setup: lets the hosted web app upload and view files in a Backblaze B2 bucket (CORS).

Backblaze's web console can only create download (view) rules, and an ordinary application key
can't change bucket settings, so run this once with the account's *master* application key.
The key is typed in here and sent only to Backblaze; it is not saved anywhere.

Usage:  python backend/scripts/set_b2_cors.py
Standard library only (no installs).
"""

import base64
import getpass
import json
import sys
import urllib.error
import urllib.request

DEFAULT_ORIGIN = "https://travelogic-supplier-hub.onrender.com"


def call(url, body=None, headers=None):
    request = urllib.request.Request(
        url, data=json.dumps(body).encode() if body is not None else None,
        headers={"Content-Type": "application/json", **(headers or {})})
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.load(response)
    except urllib.error.HTTPError as error:
        if error.code == 401:
            sys.exit("Backblaze rejected the keyID or key (401). Copy both again from the Master Application Key "
                     "section of the Application Keys page; the keyID is shown next to it.")
        try:
            detail = json.load(error).get("message") or error.reason
        except ValueError:
            detail = error.reason
        sys.exit(f"Backblaze refused the request ({error.code}): {detail}")


def ask(prompt, secret=False):
    """Asks until the answer isn't empty."""
    while True:
        answer = (getpass.getpass(prompt) if secret else input(prompt)).strip()
        if answer:
            return answer
        print("  This is required, please paste it in.")


def main():
    print("Set the upload/view rule (CORS) on a Backblaze B2 bucket.\n")
    key_id = ask("Master application keyID: ")
    key = ask("Master application key (hidden as you type): ", secret=True)
    bucket_name = ask("Bucket name (e.g. travelogic-supplier-media-bp): ")
    while True:
        origin = input(f"Web app address (press Enter for {DEFAULT_ORIGIN}): ").strip().rstrip("/") or DEFAULT_ORIGIN
        if origin.startswith(("https://", "http://")) and "backblazeb2.com" not in origin:
            break
        print("  This is the website's address (where people open the app), not the storage endpoint.")

    credentials = base64.b64encode(f"{key_id}:{key}".encode()).decode()
    account = call("https://api.backblazeb2.com/b2api/v2/b2_authorize_account",
                   headers={"Authorization": f"Basic {credentials}"})
    auth = {"Authorization": account["authorizationToken"]}
    api = account["apiUrl"] + "/b2api/v2"

    buckets = call(f"{api}/b2_list_buckets", {"accountId": account["accountId"], "bucketName": bucket_name}, auth)["buckets"]
    if not buckets:
        sys.exit(f"No bucket named '{bucket_name}' in this account.")

    rule = {
        "corsRuleName": "supplier-hub",
        "allowedOrigins": [origin],
        # Upload (PUT with a signed URL), view and check files through the S3-compatible API.
        "allowedOperations": ["s3_put", "s3_get", "s3_head"],
        "allowedHeaders": ["*"],
        "exposeHeaders": ["ETag"],
        "maxAgeSeconds": 3600,
    }
    updated = call(f"{api}/b2_update_bucket",
                   {"accountId": account["accountId"], "bucketId": buckets[0]["bucketId"], "corsRules": [rule]}, auth)

    print(f"\nDone. Bucket '{updated['bucketName']}' now allows uploads and viewing from {origin}.")


if __name__ == "__main__":
    main()
