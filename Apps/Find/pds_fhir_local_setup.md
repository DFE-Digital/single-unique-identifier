# Setup Fhir PDS credentials locally

## Setting up an API credential

1. On the [NHS Developer site](https://onboarding.prod.api.platform.nhs.uk/), create a new developer account, if you do not yet have one.
2. Add a new application by going to environment access. Use the 'Integration test' and set yourself as the owner.
3. Create a new API, linked to `Personal Demographics Service - Application-Restricted (Integration Testing)`. You will need to give this a globally unique name.
4. Generate a key pair, by running these commands (from [the NHS documentation](https://digital.nhs.uk/developer/guides-and-documentation/security-and-authorisation/application-restricted-restful-apis-signed-jwt-authentication))
    1. ```
       KID=test-1
       ```
    2. ```
       openssl genrsa -out $KID.pem 4096
       ```
    3. ```
       openssl rsa -in $KID.pem -pubout -outform PEM -out $KID.pem.pub
       ```
    4. ```
       MODULUS=$(
       openssl rsa -pubin -in $KID.pem.pub -noout -modulus `# Print modulus of public key` \
       | cut -d '=' -f2                                    `# Extract modulus value from output` \
       | xxd -r -p                                         `# Convert from string to bytes` \
       | openssl base64 -A                                 `# Base64 encode without wrapping lines` \
       | sed 's|+|-|g; s|/|_|g; s|=||g'                    `# URL encode as JWK standard requires`
       )
       ```
    5. ```
       echo '{
         "keys": [
           {
             "kty": "RSA",
             "n": "'"$MODULUS"'",
             "e": "AQAB",
             "alg": "RS512",
             "kid": "'"$KID"'",
             "use": "sig"
           }
         ]
       }' > $KID.json
       ```
5. Upload the `test-1.json` file to your application's registration, on the 'Manage public key' page
6. On the API portal, create a new API key, and copy the _Key_ value. You do not need the secret. Then run the following command, using your Key value between the quotation marks.
    1. `API_KEY="YourKeyHere"`
7. Create the `.env` file by running the following commands:
    * The recommended location for the `.env` file is in the same directory as the `.csproj` file for the host application, for example: `Apps/Find/src/SUI.Find.FindApi/.env`
    1. `echo "export NhsAuthConfig__NHS_DIGITAL_PRIVATE_KEY=\"$(openssl rsa -in $KID.pem -traditional -out -)\"" > .env`
    2. `echo "export NhsAuthConfig__NHS_DIGITAL_KID=\"$KID\"" >> .env`
    3. `echo "export NhsAuthConfig__NHS_DIGITAL_CLIENT_ID=\"$API_KEY\"" >> .env`

## Troubleshooting

If you experience intermittent errors because these config values aren't loading correctly,
especially when using Azure Functions locally, check that the same config values are **not**
in your Azure Function App's `local.settings.json` file.

For example, check your `Apps/Find/src/SUI.Find.FindApi/local.settings.json` file,
and **remove** these lines:
```
// REMOVE these lines from local.settings.json:
"NhsAuthConfig:NHS_DIGITAL_CLIENT_ID": "",
"NhsAuthConfig:NHS_DIGITAL_KID": "",
"NhsAuthConfig:NHS_DIGITAL_PRIVATE_KEY": "",
```
