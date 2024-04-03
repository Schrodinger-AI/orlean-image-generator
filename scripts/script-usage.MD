# Image Generation test Script

This script is used for regression testing of Image Generation on Orleans
1. [Configure API Keys for Dalle and Azure OpenAI services - POST API Request](./add-api-keys.sh)
2. [Set Prompt template config for Image Generation - POST API Request](./set-prompt-config.sh)
3. [Generate Images via POST API Request](./generate-images.sh)

## Prerequisites
- Ensure the API keys for Dalle and Azure OpenAI services are configured
- Ensure the Prompt template config is set

## Steps to run the script

1. Run the script `add-api-keys.sh` to configure the API keys for Dalle and Azure OpenAI services
```sh
./add-api-keys.sh
```

2.Run the script `set-prompt-config.sh` to set the Prompt template config for Image Generation
```sh
./set-prompt-config.sh 
```

3. Run the script `generate-images.sh` to generate images via POST API Request
```sh
./generate-images.sh
```
