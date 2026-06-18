const storageKey = "bongo-cat-tracker-v1";

const initialState = {
  clicks: 0,
  keys: 0,
  keyMap: {},
};

const state = loadState();
const clickTimes = [];
const keyTimes = [];

const cat = document.querySelector(".cat");
const catWrap = document.querySelector("#catWrap");
const clickCount = document.querySelector("#clickCount");
const keyCount = document.querySelector("#keyCount");
const cpsCount = document.querySelector("#cpsCount");
const kpsCount = document.querySelector("#kpsCount");
const lastAction = document.querySelector("#lastAction");
const keyList = document.querySelector("#keyList");
const uniqueKeys = document.querySelector("#uniqueKeys");
const resetButton = document.querySelector("#resetButton");

let animationToken = 0;

render();
setInterval(renderRates, 250);

window.addEventListener("pointerdown", (event) => {
  if (event.target.closest("button")) {
    return;
  }

  state.clicks += 1;
  clickTimes.push(Date.now());
  lastAction.textContent = event.button === 2 ? "Clique direito" : "Clique";
  playHit(event.button === 2 ? "right" : "left", "clicking");
  persist();
  render();
});

window.addEventListener("contextmenu", (event) => {
  event.preventDefault();
});

window.addEventListener("keydown", (event) => {
  if (event.repeat) {
    return;
  }

  const key = normalizeKey(event);
  state.keys += 1;
  state.keyMap[key] = (state.keyMap[key] || 0) + 1;
  keyTimes.push(Date.now());
  lastAction.textContent = `Tecla ${key}`;
  playHit("key", "typing");
  persist();
  render();
});

resetButton.addEventListener("click", () => {
  state.clicks = 0;
  state.keys = 0;
  state.keyMap = {};
  clickTimes.length = 0;
  keyTimes.length = 0;
  lastAction.textContent = "Contadores zerados";
  persist();
  render();
});

function loadState() {
  try {
    const saved = JSON.parse(localStorage.getItem(storageKey));
    return {
      ...initialState,
      ...saved,
      keyMap: saved?.keyMap || {},
    };
  } catch {
    return { ...initialState, keyMap: {} };
  }
}

function persist() {
  localStorage.setItem(storageKey, JSON.stringify(state));
}

function normalizeKey(event) {
  if (event.code === "Space") {
    return "Espaco";
  }

  if (event.key.length === 1) {
    return event.key.toUpperCase();
  }

  const names = {
    ArrowUp: "Cima",
    ArrowDown: "Baixo",
    ArrowLeft: "Esquerda",
    ArrowRight: "Direita",
    Escape: "Esc",
    Backspace: "Backspace",
    Enter: "Enter",
    Tab: "Tab",
    Shift: "Shift",
    Control: "Ctrl",
    Alt: "Alt",
  };

  return names[event.key] || event.key;
}

function playHit(side, mode) {
  animationToken += 1;
  const token = animationToken;

  cat.classList.remove("hit-left", "hit-right", "hit-key", "is-clicking", "is-typing");
  catWrap.classList.remove("spark");
  void cat.offsetWidth;

  cat.classList.add(`hit-${side}`, `is-${mode}`);
  catWrap.classList.add("spark");

  window.setTimeout(() => {
    if (token !== animationToken) {
      return;
    }

    cat.classList.remove("hit-left", "hit-right", "hit-key", "is-clicking", "is-typing");
    catWrap.classList.remove("spark");
  }, 170);
}

function render() {
  clickCount.textContent = state.clicks.toLocaleString("pt-BR");
  keyCount.textContent = state.keys.toLocaleString("pt-BR");
  uniqueKeys.textContent = `${Object.keys(state.keyMap).length} unicas`;
  renderRates();
  renderKeys();
}

function renderRates() {
  const now = Date.now();
  prune(clickTimes, now);
  prune(keyTimes, now);
  cpsCount.textContent = (clickTimes.length / 5).toFixed(1);
  kpsCount.textContent = (keyTimes.length / 5).toFixed(1);
}

function prune(items, now) {
  while (items.length && now - items[0] > 5000) {
    items.shift();
  }
}

function renderKeys() {
  const sorted = Object.entries(state.keyMap)
    .sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
    .slice(0, 10);

  keyList.innerHTML = "";

  if (!sorted.length) {
    const empty = document.createElement("li");
    empty.textContent = "Digite para comecar";
    keyList.append(empty);
    return;
  }

  for (const [key, count] of sorted) {
    const item = document.createElement("li");
    item.textContent = key;

    const value = document.createElement("span");
    value.textContent = count.toLocaleString("pt-BR");
    item.append(value);

    keyList.append(item);
  }
}
