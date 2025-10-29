const API_ENDPOINT = '/chatbot/message';
const MAX_HISTORY = 20;

let history = [];

// ------ utilidades ------
function addMessage(role, text) {
    const box = document.getElementById('tos-chatbot-messages');
    const msg = document.createElement('div');
    msg.className = `tos-msg ${role === 'user' ? 'tos-msg--user' : 'tos-msg--bot'}`;
    msg.textContent = text;
    box.appendChild(msg);
    box.scrollTop = box.scrollHeight;
    history.push({ role, content: text });
    if (history.length > MAX_HISTORY) history.shift();
}

function showTyping(on) {
    const t = document.getElementById('tos-typing');
    if (!t) return;
    if (on) t.removeAttribute('hidden'); else t.setAttribute('hidden', '');
}

// ------ backend ------
async function getBotReply(query) {
    const res = await fetch(API_ENDPOINT, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text: query, history: history.slice(-6), locale: 'es-MX' })
    });
    if (!res.ok) throw new Error(`api_error_${res.status}`);
    const data = await res.json();
    return data.message || 'No pude responder ahora.';
}

// ------ enviar ------
async function sendUser(text) {
    const input = document.getElementById('tos-chatbot-input');
    if (text === undefined) text = input.value.trim();
    if (!text) return;

    input.value = '';
    addMessage('user', text);

    // mostrar “escribiendo…”
    showTyping(true);

    try {
        const reply = await getBotReply(text);
        addMessage('assistant', reply);
    } catch (err) {
        console.error('[Chubby] error', err);
        addMessage('assistant', 'Hubo un problema al responder.');
    } finally {
        // ocultar “escribiendo…” siempre
        showTyping(false);
    }
}

// ------ abrir/cerrar ------
function openClose(open) {
    const btn = document.getElementById('tos-chatbot-toggle');
    const win = document.getElementById('tos-chatbot-window');
    if (!win) return;

    if (open === undefined) open = win.hasAttribute('hidden');
    if (open) {
        win.removeAttribute('hidden');
        win.style.opacity = '1';
        win.style.transform = 'scale(1) translateY(0)';
        win.style.pointerEvents = 'auto';
        btn?.setAttribute('aria-expanded', 'true');
    } else {
        win.setAttribute('hidden', '');
        btn?.setAttribute('aria-expanded', 'false');
    }
}

// ------ cerrar al hacer clic fuera ------
function setupClickOutsideToClose() {
    document.addEventListener('click', (ev) => {
        const win = document.getElementById('tos-chatbot-window');
        const toggle = document.getElementById('tos-chatbot-toggle');
        if (!win || win.hasAttribute('hidden')) return;

        const target = ev.target;
        const insideWin = win.contains(target);
        const onToggle = toggle && toggle.contains(target);

        if (!insideWin && !onToggle) openClose(false);
    });

    const win = document.getElementById('tos-chatbot-window');
    win?.addEventListener('click', (e) => e.stopPropagation());
}

// ------ init ------
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('tos-chatbot-toggle')?.addEventListener('click', () => openClose());
    document.getElementById('tos-chatbot-close')?.addEventListener('click', () => openClose(false));
    document.getElementById('tos-chatbot-form')?.addEventListener('submit', (e) => { e.preventDefault(); sendUser(); });
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') openClose(false); });

    setupClickOutsideToClose();

    // bienvenida
    addMessage('assistant', '¡Hola! Soy Chubby 👋 ¿En qué te ayudo hoy?');
});
