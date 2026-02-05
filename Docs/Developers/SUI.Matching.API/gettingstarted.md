# SUI.Matching.API - Getting started

## Setting up an API credential

1. On the [NHS Developer site](https://onboarding.prod.api.platform.nhs.uk/), create a new developer account, if you do
   not yet have one.
2. Add a new application by going to environment access. Use the 'Integration test' and set yourself as the owner.
3. Create a new API, linked to `Personal Demographics Service - Application-Restricted (Integration Testing)`. You will
   need to give this a globally unique name.
4. Generate a keypair, by running these commands (
   from [the NHS documentation](https://digital.nhs.uk/developer/guides-and-documentation/security-and-authorisation/application-restricted-restful-apis-signed-jwt-authentication))
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
6. On the API portal, create a new API key, and copy the _Key_ value. You do not need the secret. Then run the following
   command, using your Key value between the quotation marks.
    1. `API_KEY="IlmDF45AbP8Ao11pRtkK7tCoApApdABC"`
7. Create the `.env` file to provide these secrets to the mocked Azure Secret Manager, by running the following
   commands:
    1. `echo "export NhsAuthConfig__NHS_DIGITAL_PRIVATE_KEY=\"$(openssl rsa -in $KID.pem -traditional -out -)\"" > .env`
    2. `echo "export NhsAuthConfig__NHS_DIGITAL_KID=\"$KID\"" >> .env`
    3. `echo "export NhsAuthConfig__NHS_DIGITAL_CLIENT_ID=\"$API_KEY\"" >> .env`

## Pre-requisites

You must install the .net CLI and v10 SDK. For macOS, run:

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.102 --install-dir "$HOME/.dotnet"
echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc && source ~/.zshrc && echo $PATH
```

You should then be able to build the solution, using

```bash
cd Apps/Matching
dotnet build
```

## Unit and integration testing

```bash
cd Apps/Matching
dotnet test
```

## Running locally

To build and run the project:

```bash
cd Apps/Matching
dotnet build
dotnet run --project src/SUI.Matching.API
```

Run simple test:

```bash
curl -H 'Content-Type: application/json' \
      -d '{ "given":"octavia","family":"chislett", "birthdate": "2008-09-20"}' \
      -X POST \
      http://localhost:5003/api/v1/matchperson
```

