# AZURE OpenAI Integration Docs

## Introduction

Azure OpenAI API is 2nd Service provider used by schrodinger - Orlean Image Generator Service
This document provides the details of the integration of Azure OpenAI API with schrodinger.

## Azure OpenAI references

- [Azure OpenAI API - C#](https://learn.microsoft.com/en-us/azure/ai-services/openai/dall-e-quickstart?tabs=dalle3%2Ccommand-line&pivots=programming-language-csharp)

## Pre-requisites

- An Azure subscription - Create one for free
- Access granted to Azure OpenAI Service in the desired Azure subscription. Currently, access to this service is granted only by application. You can apply for access to Azure OpenAI Service by completing the form at https://aka.ms/oai/access.
- The .NET 7 SDK
- An Azure OpenAI resource created in the East US region. For more information, see Create a resource and deploy a model with Azure OpenAI.


## Sample Execution using Python

```py
#!/usr/bin/env python
# coding: utf-8

# In[10]:


# Note: DALL-E 3 requires version 1.0.0 of the openai-python library or later
import os
from openai import AzureOpenAI
import json

os.environ['AZURE_OPENAI_API_KEY'] = ''


# In[11]:


client = AzureOpenAI(
    api_version="2024-02-01",
    azure_endpoint="https://schrodinger-east-us.openai.azure.com/",
    api_key=os.environ["AZURE_OPENAI_API_KEY"],
)


# In[12]:


prompt = 'I NEED to test how the tool works with extremely simple prompts. DO NOT add any detail, just use it AS-IS: A pixel art image of a Maine Coon cat standing like a human with both feet visible on the ground, facing directly at the viewer,. The main character is wearing Fur Hoodie, Belt with Octopus-shaped buckle, and Quill and Inkwell Necklace. The image has a Pastel Stripes background. The image should contain the full-body shot of the main character. The image should contain one and only one cat. The generated image should not contain any text or labels.'


# In[18]:


result = client.images.generate(
    model="Dalle3",
    prompt=prompt,
    n=1
)


# In[19]:


result.dict()


# In[20]:


image_url = json.loads(result.model_dump_json())['data'][0]['url']


# In[21]:


print(image_url)

```


## Sample Execution using C#

```C#



```


## Content Violation Categories

https://learn.microsoft.com/en-us/azure/ai-services/openai/concepts/content-filter?tabs=warning%2Cpython-new

