// @ts-check
// https://codepen.io/cleveryeti/embed/JjwNqgP
"strict"

class Timer {
    constructor(notiEl, duration) {
        this.notiEl = notiEl;
        this.duration = duration * 1000;
        this.startTime = Date.now();
        this.remainingTime = this.duration;
        this.timerId = null;
        this.start();
    }

    start() {
        this.notiEl.querySelector('.timeout-bar').style.animationPlayState = 'running';
        this.startTime = Date.now();
        this.timerId = setTimeout(() => this.finish(), this.remainingTime);
    }

    pause() {
        if( this.timerId ) {
            clearTimeout(this.timerId);
        }
        this.notiEl.querySelector('.timeout-bar').style.animationPlayState = 'paused';
        const elapsedTime = Date.now() - this.startTime;
        this.remainingTime -= elapsedTime;
    }

    resume() {
        this.start();
    }

    finish() {
        this.notiEl.classList.add("out");
        this.notiEl.addEventListener("animationend", () => {
            setTimeout(() => {
                this.notiEl.remove();
            }, 1000);
            Notifications.timers = Notifications.timers.filter(timer => timer !== this);
        });
    }
}


export class Notifications {

    static timers = []

    constructor(el) {
        this.el = el;
    }

    // function to create new elements with a class (cleans up code)
    createDiv(className = "") {
        const el = document.createElement("div");
        el.classList.add(className);
        return el;
    }
    // function to add text nodes to elements
    addText(el, text) {
        el.appendChild(document.createTextNode(text));
    }

    addDiv(el, text) {
        let tempContainer = document.createElement('div');
        tempContainer.innerHTML = text;
        let divElement = tempContainer.firstChild;
        el.appendChild(tempContainer);
    }

    create(
        title = "Untitled notification",
        ex_class_name = "",
        description = "",
        duration = 2,
        destroyOnClick = false,
        clickFunction = undefined
    ) {
        // functions
        function destroy(animate) {
            if (animate) {
                notiEl.classList.add("out");
                notiEl.addEventListener("animationend", () => {
                    notiEl.remove();
                });
            } else {
                notiEl.remove();
            }
        }

        // create the elements and add their content
        const notiEl = this.createDiv("noti");
        const notiCardEl = this.createDiv("noticard");
        if( ex_class_name ) {
            notiCardEl.classList.add(ex_class_name.toLowerCase())
        }
        const glowEl = this.createDiv("notiglow");
        const borderEl = this.createDiv("notiborderglow");

        const titleEl = this.createDiv("notititle");
        this.addText(titleEl, title);

        const descriptionEl = this.createDiv("notidesc");
        this.addDiv(descriptionEl, description);

        // append the elements to each other
        notiEl.appendChild(notiCardEl);
        notiCardEl.appendChild(glowEl);
        notiCardEl.appendChild(borderEl);
        notiCardEl.appendChild(titleEl);
        notiCardEl.appendChild(descriptionEl);

        this.el.appendChild(notiEl);

        // transition the height of the container to the height of the visible card
        // console.log("height", notiCardEl.scrollHeight);
        requestAnimationFrame(function () {
            notiEl.style.height =
                "calc(0.25rem + " +
                notiCardEl.getBoundingClientRect().height +
                "px)";
        });

        // hover animation
        notiEl.addEventListener("mousemove", (event) => {
            const rect = notiCardEl.getBoundingClientRect();
            const localX = (event.clientX - rect.left) / rect.width;
            const localY = (event.clientY - rect.top) / rect.height;

            // console.log(localX, localY);
            glowEl.style.left = localX * 100 + "%";
            glowEl.style.top = localY * 100 + "%";

            borderEl.style.left = localX * 100 + "%";
            borderEl.style.top = localY * 100 + "%";
        });

        // onclick function if one is set
        if (clickFunction != undefined) {
            notiEl.addEventListener("click", clickFunction);
        }

        // destroy the notification on click if it is set to do so
        if (destroyOnClick) {
            notiEl.addEventListener("click", () => destroy(true));
        }

        const timeoutBar = this.createDiv("timeout-bar");
        notiCardEl.appendChild(timeoutBar);
        timeoutBar.style.animationDuration = `${duration}s`;

        // remove the notification after the set time if there is one
        if (duration != 0) {
            const timer = new Timer(notiEl, duration);
            Notifications.timers.push(timer);
        }
        return notiEl;
    }

    pause() {
        Notifications.timers.forEach(timer => timer.pause());
    }

    unpause() {
        Notifications.timers.forEach(timer => timer.resume());
    }
}

/*
How to use:

create the notification object using new Notifications and pass it the notification wrapper element
ex: const notis = new Notifications(document.querySelector(".notifications"))

make notifications by using notis.create() (or *.create() if you named it something else)

notis.create() parameters:
Title: string,
Description: string,
Duration: seconds (default: 2s, 0 makes it stay forever),
Destroy on click: boolean
(determines if the notification should disappear when clicked, default: false)
Click function: function
(gets called when the notification is clicked if it isnt undefined, default: undefined)
*/
