document.addEventListener('DOMContentLoaded', () => {
    const body = document.body;
    const dayNightToggle = document.getElementById('day-night-toggle');
    const colorCycleToggle = document.getElementById('color-cycle-toggle');

    // Day/Night Mode
    const savedTheme = localStorage.getItem('truckdeck-theme') || 'theme-night';
    body.className = savedTheme;
    updateThemeIcon();

    dayNightToggle.addEventListener('click', () => {
        if (body.classList.contains('theme-night')) {
            body.classList.replace('theme-night', 'theme-day');
            localStorage.setItem('truckdeck-theme', 'theme-day');
        } else {
            body.classList.replace('theme-day', 'theme-night');
            localStorage.setItem('truckdeck-theme', 'theme-night');
        }
        updateThemeIcon();
    });

    function updateThemeIcon() {
        dayNightToggle.innerText = body.classList.contains('theme-night') ? '🌙' : '☀️';
    }

    // Color Cycling
    const accents = [
        { hex: '#b6ff1f', rgb: '182, 255, 31' }, // Lime (Default)
        { hex: '#1fb6ff', rgb: '31, 182, 255' }, // Blue
        { hex: '#ff1f66', rgb: '255, 31, 102' }, // Pink
        { hex: '#ffb61f', rgb: '255, 182, 31' }, // Orange
        { hex: '#1fffb6', rgb: '31, 255, 182' }  // Teal
    ];

    let currentAccentIndex = parseInt(localStorage.getItem('truckdeck-accent-index') || '0');
    applyAccent(currentAccentIndex);

    colorCycleToggle.addEventListener('click', () => {
        currentAccentIndex = (currentAccentIndex + 1) % accents.length;
        applyAccent(currentAccentIndex);
        localStorage.setItem('truckdeck-accent-index', currentAccentIndex);
    });

    function applyAccent(index) {
        const accent = accents[index];
        document.documentElement.style.setProperty('--accent', accent.hex);
        document.documentElement.style.setProperty('--accent-rgb', accent.rgb);
    }
});
