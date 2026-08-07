const canvas = document.getElementById('canvas') as HTMLCanvasElement;
const ctx = canvas.getContext('2d')!;

// Set canvas size
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

let mouseX = 0;
let mouseY = 0;

// Update mouse position
document.addEventListener('mousemove', (event) => {
    mouseX = event.clientX;
    mouseY = event.clientY;
    drawLines();
});

// Draw lines from boxes to mouse position
function drawLines() {
    const boxes = document.querySelectorAll('.card.highlight-effect');

    ctx.clearRect(0, 0, canvas.width, canvas.height); // Clear canvas

    boxes.forEach(box => {
        const rect = box.getBoundingClientRect();
        const boxX = rect.left + rect.width / 2;
        const boxY = rect.top + rect.height / 2;

        // Adjust for the body's transformation
        const transformedBoxX = boxX;
        const transformedBoxY = boxY / Math.cos(20 * Math.PI / 180); // Adjust Y position based on rotation

        ctx.beginPath();
        ctx.moveTo(transformedBoxX, transformedBoxY);
        ctx.lineTo(mouseX, mouseY);
        ctx.strokeStyle = 'green';
        ctx.lineWidth = 2;
        ctx.stroke();
    });
}

// Resize canvas on window resize
window.addEventListener('resize', () => {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    drawLines(); // Redraw lines after resizing
});

