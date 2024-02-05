import asyncio
import websockets
import random

async def send_random_numbers():
    uri = "ws://localhost:7072/"
    
    while True:
        try:
            async with websockets.connect(uri) as websocket:
                print("Connected to WebSocket server")

                while True:
                    # Generate a random number
                    random_number = random.randint(1, 100)

                    # Send the random number to the server
                    await websocket.send(str(random_number))

                    print(f"Sent random number: {random_number}")

                    # Wait for a short interval before sending the next number
                    await asyncio.sleep(2)
        except :
            print("WebSocket connection closed. Reconnecting in 5 seconds...")
            await asyncio.sleep(5)

if __name__ == "__main__":
    asyncio.get_event_loop().run_until_complete(send_random_numbers())
