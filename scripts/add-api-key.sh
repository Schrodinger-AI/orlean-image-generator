curl -s -X 'POST' \
  'http://localhost:5069/scheduler/add' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '[
  {
    "apiKey": "'${OPENAI_API_KEY}'",
    "email": "abc@example.com",
    "tier": 1,
    "maxQuota": 5
  }
]' > /dev/null

response=$(curl -X 'GET' \
             'http://localhost:5069/scheduler' \
             -H 'accept: text/plain' 2>&1 | grep 'apiKey')

if [ -z "$response" ]; then
  echo "Failed to set config"
  exit 1
else
  echo "API Key added"
  exit 0
fi
