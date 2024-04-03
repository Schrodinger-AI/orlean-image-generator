response=$(curl -X 'POST' \
  'http://localhost:5069/image/generate' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
  "newAttributes": [
    {
      "traitType": "background",
      "value": "Stormy Seas"
    },
    {
      "traitType": "breed",
      "value": "Chantilly-Tiffany"
    },
    {
      "traitType": "clothes",
      "value": "Peplum Top"
    },
    {
      "traitType": "cap",
      "value": "Rastacap"
    },
    {
      "traitType": "face",
      "value": "Sad"
    },
    {
      "traitType": "ride",
      "value": "Scooty"
    },
    {
      "traitType": "wings",
      "value": "Moth Wings"
    }
  ],
  "baseImage": {},
  "numImages": 1
}' 2>&1 | grep 'requestId')

echo $response

requestId=$(echo $response | jq '.requestId' -r)

if [ -z "$requestId" ] || [ "$requestId" = "null" ]; then
  echo "Failed to send request, $requestId"
  exit 1
fi

sleep 60

response=$(curl -s -X 'POST' \
  'http://localhost:5069/image/query' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "requestId": "'$requestId'"
}')


image=$(echo $response | jq '.images[0].image' -r)

if [ -z "$image" ] || [ "$image" = "null" ]; then
  echo "Failed to generate image, $requestId"
  exit 1
else
  echo "Image generated"
  exit 0
fi