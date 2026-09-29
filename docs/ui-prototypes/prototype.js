const params = new URLSearchParams(location.search);
const state = {
  concept: params.get("concept") || "spatial",
  mode: params.get("state") || "mulligan",
  scale: params.get("scale") || "100",
  selected: new Set([1, 4]),
  seat: "Spider-Man",
  target: "Rhino",
  payment: new Set([1, 4]),
  revision: 1,
  submitting: false,
  receipt: false,
};
const cards = [
  {
    name: "Swinging Web Kick",
    type: "Event",
    cost: "3",
    res: "◆",
    text: "Hero Action (attack): Deal 8 damage to an enemy.",
  },
  {
    name: "Backflip",
    type: "Event",
    cost: "0",
    res: "●",
    text: "Interrupt (defense): When you would take any amount of damage from an attack, prevent all of that damage.",
  },
  {
    name: "Web-Shooter",
    type: "Upgrade",
    cost: "1",
    res: "●",
    text: "Uses (3 web counters). (Enters play with 3 counters. When those are gone, discard this card) Hero Resource: Exhaust Web-Shooter and remove 1 web counter from it → generate a wild resource.",
  },
  {
    name: "Aunt May",
    type: "Support",
    cost: "1",
    res: "✦",
    text: "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter Parker.",
  },
  {
    name: "Enhanced Spider-Sense",
    type: "Event",
    cost: "1",
    res: "◆",
    text: 'Hero Interrupt: When a treachery card is revealed from the encounter deck, cancel its "When Revealed" effects.',
  },
  {
    name: "Spider-Tracer",
    type: "Upgrade",
    cost: "1",
    res: "✦",
    text: "Attach to a minion. Forced Interrupt: When attached minion is defeated, remove 3 threat from a scheme.",
  },
];
const actions = [
  "Basic attack",
  "Basic thwart",
  "Change to alter-ego",
  "Play Swinging Web Kick",
  "Play Web-Shooter",
  "Play Aunt May",
  "Play Spider-Tracer",
  "Black Cat: attack",
  "Black Cat: thwart",
  "Mockingbird: attack",
  "Mockingbird: thwart",
  "End turn",
];
const names = {
  spatial: "A · Spatial table",
  atlas: "B · Decision atlas",
  guided: "C · Guided intent",
};
const app = document.querySelector("#app");
const button = (text, attrs = "") => `<button ${attrs}>${text}</button>`;
function switcher() {
  return `<div class="seat-switch">${button("Spider-Man · answering", `data-seat="Spider-Man" aria-pressed="${state.seat === "Spider-Man"}"`)}${button("Captain Marvel", `data-seat="Captain Marvel" aria-pressed="${state.seat === "Captain Marvel"}"`)}</div>`;
}
function result() {
  return state.mode === "result" || state.receipt
    ? `<div class="result" aria-live="polite"><strong>✓ Resolved</strong><span>Spider-Man changed to hero form.</span>${button("View history", "data-history")}</div>`
    : "";
}
function object(title, kind, big, facts, act, hero = false) {
  return `<article class="object ${hero ? "hero" : ""}"><span class="eyebrow">${kind}</span><h3>${title}</h3><div class="big">${big}</div><div class="facts">${facts}</div>${button(act, `data-object="${title}"`)}</article>`;
}
function world() {
  return `<section class="panel world"><div class="panel-head"><div><span class="eyebrow">Shared encounter</span><h2>Rhino is breaking in</h2></div>${button("Piles & attachments", "data-piles")}</div><div class="object-grid">${object("Rhino", "Villain · Stage I", "11 <small>/ 14 health</small>", "<span>SCH 1</span><span>ATK 2</span>", state.mode === "target" ? "✓ Target Rhino" : "Inspect Rhino")}${object("The Break-In!", "Main scheme", "3 <small>/ 7 threat</small>", "<span>Acceleration 1</span>", "Inspect scheme")}${object("Hydra Mercenary", "Engaged minion", "3 <small>/ 3 health</small>", "<span>Guard</span><span>ATK 1</span>", state.mode === "target" ? "Choose target" : "Inspect minion")}${object(state.seat, "Identity · Ready", "8 <small>/ 10 health</small>", "<span>ATK 2</span><span>THW 1</span><span>DEF 3</span>", "View identity", true)}${object("Black Cat", "Ally · Ready", "2 <small>/ 2 health</small>", "<span>ATK 1</span><span>THW 1</span>", "2 offered actions", true)}${object("Web-Shooter", "Upgrade · Ready", "2 <small>web counters</small>", "<span>Resource ability</span>", "View ability", true)}</div>${result()}</section>`;
}
function hand(ledger = false) {
  const other = state.seat !== "Spider-Man";
  return `<section class="panel hand"><div class="panel-head"><h2>${state.seat} · ${other ? "concealed hand" : "Hand · 6"}</h2>${state.concept === "spatial" ? "" : switcher()}</div>${
    other
      ? "<p>5 concealed cards. This viewer is not authorized to inspect them.</p>"
      : `<div class="${ledger ? "hand-ledger" : "hand-grid"}">${cards
          .map((card, i) => {
            const selected =
              state.mode === "mulligan"
                ? state.selected.has(i)
                : state.mode === "target" && state.payment.has(i);
            const label =
              state.mode === "mulligan"
                ? selected
                  ? "✓ Replace"
                  : "Keep → Replace"
                : state.mode === "target"
                  ? selected
                    ? "✓ Payment"
                    : "Use for payment"
                  : "View card";
            return ledger
              ? `<div class="inventory-row ${selected ? "selected" : ""}" data-source="${i}"><span class="resource" aria-label="printed resource">${card.res}</span><div class="item-title"><strong>${card.name}</strong><span>${card.type} · cost ${card.cost}</span></div>${button("Read", `data-inspect="${i}"`)}${button(state.mode === "mulligan" ? (selected ? "✓ Replace" : "Keep") : state.mode === "target" ? (selected ? "✓ Pay" : "Pay") : "Details", `data-card="${i}" aria-pressed="${!!selected}"`)}</div>`
              : `<article class="hand-item ${selected ? "selected" : ""}" data-source="${i}"><h3>${card.name}</h3><span class="card-meta">${card.type} · Cost ${card.cost} · ${card.res}</span><div class="card-text">${card.text}</div>${button("Read card", `class="inspect" data-inspect="${i}"`)}${button(label, `class="choose" data-card="${i}" aria-pressed="${!!selected}"`)}</article>`;
          })
          .join("")}</div>`
  }</section>`;
}
function table() {
  return `<section class="panel"><div class="panel-head"><div><span class="eyebrow">Authorized state</span><h2>Encounter & active player</h2></div>${button("All areas", "data-piles")}</div><table class="state-table"><thead><tr><th>Object</th><th>Live state</th><th>Related choices</th></tr></thead><tbody><tr><td>Rhino I</td><td>11/14 health</td><td>${button("Target", 'data-object="Rhino"')}</td></tr><tr><td>The Break-In!</td><td>3/7 threat</td><td>${button("Read", 'data-object="The Break-In!"')}</td></tr><tr><td>Hydra Mercenary</td><td>3/3 · Guard</td><td>${button("Target", 'data-object="Hydra Mercenary"')}</td></tr><tr><td>${state.seat}</td><td>8/10 · Ready</td><td>${button("Read", 'data-object="Spider-Man"')}</td></tr><tr><td>Black Cat</td><td>2/2 · Ready</td><td>2 actions</td></tr><tr><td>Web-Shooter</td><td>2 counters · Ready</td><td>1 resource</td></tr></tbody></table></section>`;
}
function choices(list = actions) {
  return `<div class="choice-list">${list.map((a, i) => button(a, `data-action="${a}"`)).join("")}</div>`;
}
function mulliganBody() {
  const chosen = [...state.selected].map((i) => cards[i].name);
  const content = `<h3>Choose cards to replace</h3><p class="help">Keep any card you do not select. Review the replacement set before confirming.</p>`;
  if (state.concept === "spatial")
    return `${content}<div class="selection-summary"><strong>${chosen.length} selected in your hand</strong><p>${chosen.join("<br>") || "No replacements selected"}</p></div><p class="help">Select cards directly in the hand. Blue rail + checkmark marks each replacement.</p>`;
  return `${content}<div class="${state.concept === "guided" ? "wide-choices" : "choice-list"}">${cards.map((c, i) => button(`${state.selected.has(i) ? "✓ Replace" : "Keep"} · ${c.name}`, `data-card="${i}" aria-pressed="${state.selected.has(i)}"`)).join("")}</div>`;
}
function actionBody() {
  if (state.concept === "guided")
    return `<div class="intent-grid"><div class="intent-group"><h3>Use your identity</h3>${choices(actions.slice(0, 3))}${button("End turn", 'data-action="End turn"')}</div><div class="intent-group"><h3>Play from hand</h3>${choices(actions.slice(3, 7))}</div><div class="intent-group"><h3>Use an ally</h3>${choices(actions.slice(7, 11))}</div></div>`;
  return `<p class="help">All 12 offered choices · grouped by source only</p>${choices()}`;
}
function targetBody() {
  return `<div class="row spread"><h3>Swinging Web Kick</h3>${button("Change action", "data-change")}</div><div class="target-composition"><div class="stack"><h3>Target</h3><div class="choice-list">${button(`${state.target === "Rhino" ? "✓ " : ""}Rhino`, 'data-target="Rhino"')}${button(`${state.target === "Hydra Mercenary" ? "✓ " : ""}Hydra Mercenary`, 'data-target="Hydra Mercenary"')}</div></div><div class="stack"><h3>Payment sources</h3>${state.concept === "spatial" ? '<p class="help">Select payment in the hand or use the offered Web-Shooter ability.</p>' : `<div class="choice-list">${[1, 4].map((i) => button(`${state.payment.has(i) ? "✓ " : ""}${cards[i].name} · ${cards[i].res}`, `data-pay="${i}" aria-pressed="${state.payment.has(i)}"`)).join("")}</div>`}${button("✓ Web-Shooter · 1 wild", 'data-generator aria-pressed="true"')}</div></div><div class="selection-summary">Draft: ${state.target}<br>${[...state.payment].map((i) => cards[i].name).join(" + ")} + Web-Shooter</div>`;
}
function decision() {
  const mulligan = state.mode === "mulligan",
    target = state.mode === "target";
  return `<section class="panel decision"><div class="intro"><span class="eyebrow">Spider-Man's decision · ${mulligan ? "Setup" : target ? "Compose action" : "Player phase"}</span><h1>${mulligan ? "Which cards will you replace?" : target ? "Choose a target and payment." : "What will you do next?"}</h1>${state.concept === "guided" ? `<div class="step-strip"><span class="${!target ? "active" : ""}">1 Choose</span><span class="${target ? "active" : ""}">2 Compose</span><span>3 Confirm</span></div>` : ""}</div><div class="decision-body">${mulligan ? mulliganBody() : target ? targetBody() : actionBody()}${state.concept !== "spatial" ? result() : ""}</div><div class="commit">${mulligan ? `<div class="receipt-line"><span>Replace</span><strong>${state.selected.size} cards</strong></div><div class="receipt-line"><span>Keep</span><strong>${6 - state.selected.size} cards</strong></div>` : target ? '<div class="receipt-line"><span>Payment</span><strong>Draft sources selected</strong></div>' : '<p class="help">Choose an action to see its targets, costs and final confirmation.</p>'}<p class="status" aria-live="polite">${state.submitting ? "Submitting this decision…" : mulligan ? "✓ Replacement set ready to confirm" : target ? "Illustrative draft · engine validation required" : "12 choices available"}</p><div class="buttons">${mulligan ? button("Keep entire hand", "data-keep") : target ? button("Cancel", "data-change") : ""}${button(mulligan ? `Replace ${state.selected.size} & draw` : target ? "Confirm action" : "Select an action", `class="primary" data-commit ${(!mulligan && !target) || state.submitting ? "disabled" : ""}`)}</div></div></section>`;
}
function context() {
  return `<section class="panel context"><div class="panel-head"><h2>Encounter overview</h2>${switcher()}${button("Full state", "data-piles")}</div><div class="stat-strip">${[
    ["Rhino I", "11 / 14 health"],
    ["The Break-In!", "3 / 7 threat"],
    ["Hydra Mercenary", "3 / 3 · Guard"],
    [state.seat, "8 / 10 · Ready"],
    ["Black Cat", "2 / 2 · Ready"],
    ["Web-Shooter", "2 counters"],
  ]
    .map(
      ([n, v]) =>
        `<div class="stat"><span>${n}</span><strong>${v}</strong></div>`,
    )
    .join("")}</div></section>`;
}
function render() {
  document.body.classList.toggle("large", state.scale === "150");
  app.className = state.concept;
  document.querySelector("#concept-name").textContent = names[state.concept];
  document.querySelector("#view-name").textContent = state.seat;
  document.querySelector("#concept").value = state.concept;
  document.querySelector("#state").value = state.mode;
  document.querySelector("#scale").value = state.scale;
  app.innerHTML =
    state.concept === "spatial"
      ? world() + hand() + decision()
      : state.concept === "atlas"
        ? `<div class="record">${table()}${hand(true)}</div>` + decision()
        : context() + decision() + hand(true);
  if (state.concept !== "spatial") {
    const resultPanel = app.querySelector('.result');
    if (resultPanel) app.querySelector('.commit').prepend(resultPanel);
  }
  if (state.concept === "guided" && state.mode === "target") {
    app.querySelector('.commit').prepend(app.querySelector('.selection-summary'));
  }
  if (state.concept === "spatial")
    app
      .querySelector(".hand .panel-head")
      .insertAdjacentHTML("beforeend", switcher());
}
function commit() {
  if (state.submitting) return;
  state.submitting = true;
  const rev = state.revision;
  render();
  setTimeout(() => {
    if (rev !== state.revision) return;
    state.submitting = false;
    state.revision++;
    state.mode = "result";
    state.receipt = true;
    render();
  }, 180);
}
let source = null;
function inspect(i, trigger) {
  const card = cards[i];
  source = trigger;
  document.querySelector("#inspector-title").textContent = card.name;
  document.querySelector("#inspector-meta").textContent =
    `${card.type} · Printed cost ${card.cost}`;
  document.querySelector("#inspector-text").textContent = card.text;
  const dialog = document.querySelector("#inspector");
  dialog.showModal();
  const sourceRect =
    trigger.closest("[data-source]")?.getBoundingClientRect() ||
    trigger.getBoundingClientRect();
  const r = dialog.getBoundingClientRect();
  let attached = state.scale !== "150" && sourceRect.top > r.height + 28;
  dialog.classList.toggle('attached', attached);
  dialog.style.top = attached
    ? `${sourceRect.top - r.height - 18}px`
    : `${Math.max(24, (innerHeight - r.height) / 2)}px`;
  dialog.style.left = attached
    ? `${Math.min(innerWidth - r.width - 24, Math.max(24, sourceRect.left + sourceRect.width / 2 - r.width / 2))}px`
    : `${(innerWidth - r.width) / 2}px`;
  document.querySelector("#inspector-origin").textContent =
    `${attached ? "From hand →" : "Card details · "}${card.name}`;
  trigger.closest("[data-source]")?.classList.add("inspector-source");
  const connector = document.querySelector("#connector");
  dialog.append(connector);
  connector.hidden = !attached;
  if (attached) {
    connector.style.width = "18px";
    connector.style.left = `${sourceRect.left + sourceRect.width / 2}px`;
    connector.style.top = `${sourceRect.top - 18}px`;
    connector.style.transform = "rotate(90deg)";
  }
  document.querySelector("#close-inspector").focus();
}
document.querySelector("#inspector").addEventListener("close", () => {
  document
    .querySelectorAll(".inspector-source")
    .forEach((el) => el.classList.remove("inspector-source"));
  document.querySelector("#connector").hidden = true;
  if (source?.isConnected) source.focus();
});
document.querySelector('#inspector').addEventListener('click', event => {
  if (event.target !== event.currentTarget) return;
  const r = event.currentTarget.getBoundingClientRect();
  if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) event.currentTarget.close();
});
document.querySelector("#close-inspector").onclick = () =>
  document.querySelector("#inspector").close();
app.addEventListener("click", (event) => {
  const el = event.target.closest("button");
  if (!el) return;
  if (el.dataset.seat) {
    state.seat = el.dataset.seat;
    render();
  } else if (el.dataset.inspect !== undefined) inspect(+el.dataset.inspect, el);
  else if (el.dataset.card !== undefined) {
    const i = +el.dataset.card;
    if (state.mode === "mulligan") {
      state.selected.has(i) ? state.selected.delete(i) : state.selected.add(i);
      render();
    } else if (state.mode === "target") {
      state.payment.has(i) ? state.payment.delete(i) : state.payment.add(i);
      render();
    } else inspect(i, el);
  } else if (el.dataset.pay !== undefined) {
    const i = +el.dataset.pay;
    state.payment.has(i) ? state.payment.delete(i) : state.payment.add(i);
    render();
  } else if (el.hasAttribute("data-commit") || el.hasAttribute("data-keep")) {
    if (el.hasAttribute("data-keep")) state.selected.clear();
    commit();
  } else if (el.dataset.action) {
    state.mode = "target";
    render();
  } else if (el.hasAttribute("data-change")) {
    state.mode = "actions";
    render();
  } else if (el.dataset.target) {
    state.target = el.dataset.target;
    render();
  } else if (el.dataset.object) {
    if (state.mode === "target") {
      state.target = el.dataset.object;
      render();
    } else
      showNote(
        "Object details",
        `${el.dataset.object}: this study shows current values in context. Production uses the authorized descriptor for full rules text and live state.`,
      );
  } else if (el.hasAttribute("data-history")) history();
  else if (el.hasAttribute("data-piles"))
    showNote(
      "All areas",
      "Encounter deck: 25 concealed cards. Player deck: 31 concealed cards. Discard: 3 public cards. Attachments and unoccupied areas remain accessible here.",
    );
});
function showNote(title, text) {
  const d = document.querySelector("#inspector");
  d.classList.remove('attached');
  source = document.activeElement;
  document.querySelector("#inspector-title").textContent = title;
  document.querySelector("#inspector-origin").textContent =
    "Additional context";
  document.querySelector("#inspector-meta").textContent = "";
  document.querySelector("#inspector-text").textContent = text;
  d.style.left = "calc(50vw - 270px)";
  d.style.top = "200px";
  d.showModal();
}
function history() {
  showNote(
    "Action history",
    "Spider-Man changed to hero form. Earlier: Spider-Man replaced 2 cards. Results are a separate chronology; they never take space from the next decision.",
  );
}
document.querySelector("#history").onclick = history;
document.querySelector("#sync").onclick = () => {
  state.revision++;
  state.submitting = false;
  render();
};
for (const id of ["concept", "state", "scale"])
  document.querySelector(`#${id}`).onchange = (e) => {
    state[id === "state" ? "mode" : id] = e.target.value;
    state.revision++;
    state.submitting = false;
    state.receipt = false;
    render();
  };
render();
