const draggables = document.querySelectorAll<HTMLElement>('.draggable');

draggables.forEach(draggable => {
    let offsetX: number, offsetY: number;

    draggable.addEventListener('mousedown', (e: MouseEvent) => {
        offsetX = e.clientX - draggable.getBoundingClientRect().left;
        offsetY = e.clientY - draggable.getBoundingClientRect().top;

        document.addEventListener('mousemove', mouseMoveHandler);
        document.addEventListener('mouseup', mouseUpHandler);
    });

    const mouseMoveHandler = (e: MouseEvent) => {
        draggable.style.left = `${e.clientX - offsetX}px`;
        draggable.style.top = `${e.clientY - offsetY}px`;
    };

    const mouseUpHandler = () => {
        document.removeEventListener('mousemove', mouseMoveHandler);
        document.removeEventListener('mouseup', mouseUpHandler);
    };
});

export function centerDraggableDiv(draggable: HTMLElement) {
    const bodyWidth = document.body.clientWidth;
    const divWidth = draggable.clientWidth;
    const centerX = (bodyWidth - divWidth) / 2;
    
    draggable.style.left = `${centerX}px`;
}

