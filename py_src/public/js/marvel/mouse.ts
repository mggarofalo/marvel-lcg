import { Client } from "./client.js";

function updatePlayerMousePosition(playerId: string, xPercent: number, yPercent: number) {
    // Logic to update the UI, e.g., draw a dot at (x, y) for the player with playerId
    // debugger
    const x = (xPercent / 100) * MouseSync.rect.width + MouseSync.rect.left;  // Convert percentage to pixel value
    const y = (yPercent / 100) * MouseSync.rect.height + MouseSync.rect.top; // Convert percentage to pixel value

    const playerMarker = document.getElementById(`player-mouse-${playerId}`);
    if (playerMarker) {
        playerMarker.style.left = `${x}px`;
        playerMarker.style.top = `${y}px`;
        playerMarker.style.position = 'absolute'; // Ensure the marker is positioned absolutely
    }
    console.log("Render", playerId, xPercent, yPercent)
}

export class MouseSync {

    static scene = document.getElementById('scene')!;
    static rect = MouseSync.scene.getBoundingClientRect();
    static is_init = false

    static init() {
        // document.addEventListener('mousemove', (event) => {
        //     if( !MouseSync.is_init ) {
        //         MouseSync.rect = MouseSync.scene.getBoundingClientRect();
        //         MouseSync.is_init = true
        //     }

        //     if (Client.ws.ws) {
        //         const xPercent = ((event.clientX - MouseSync.rect.left) / MouseSync.rect.width) * 100;
        //         const yPercent = ((event.clientY - MouseSync.rect.top) / MouseSync.rect.height) * 100;

        //         console.log("Send", event.clientY, MouseSync.rect.top)

        //         const mousePosition = {
        //             mouse_position: {
        //                 x: xPercent,
        //                 y: yPercent,
        //             }
        //         };

        //         if (Client.ws.ws && Client.ws.ws.readyState === WebSocket.OPEN) {
        //             console.log("Send", xPercent, yPercent)

        //             Client.ws.ws.send(JSON.stringify(mousePosition));
        //         } else {
        //             console.warn("WebSocket is not open. Message not sent:", mousePosition);
        //         }
        //     }
        // });
    }

    static handle(parsedMessage: any) {
        // Update other players' mouse positions
        interface Positions {
            player_id: string
            x: number;
            y: number;
        }
        // debugger
        for( const positions of parsedMessage.mouse_position ) {
            updatePlayerMousePosition(positions.player_id, positions.x, positions.y);
        }
    }
}
