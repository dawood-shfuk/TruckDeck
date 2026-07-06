(function () {
    'use strict';

    const API = '/api/maps';
    let statusData = null;
    let pollTimer = null;
    let activeJobId = null;
    let pendingGames = [];

    const $ = (sel) => document.querySelector(sel);

    async function api(path, options) {
        const res = await fetch(API + path, Object.assign({
            headers: { 'Content-Type': 'application/json' }
        }, options || {}));
        const data = await res.json().catch(() => ({}));
        if (!res.ok) throw new Error(data.error || res.statusText || 'Request failed');
        return data;
    }

    function formatBytes(n) {
        if (!n) return '—';
        if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB';
        return (n / (1024 * 1024)).toFixed(1) + ' MB';
    }

    function setFieldStatus(game, valid, message) {
        const el = $(game === 'ats' ? '#ats-status' : '#ets2-status');
        el.textContent = message || (valid ? 'Valid game folder' : 'Invalid — base.scs not found');
        el.className = 'field-status ' + (valid ? 'ok' : 'bad');
    }

    function activeTools(data) {
        if (data.runtime === 'wsl') return data.wslTools || {};
        return data.nativeTools || {};
    }

    function renderRuntimeBadge(data) {
        const badge = $('#runtime-badge');
        if (!data.wsl || !data.wsl.available) {
            badge.textContent = 'Backend: WSL not installed';
            badge.className = 'runtime-badge bad';
            return;
        }
        const distro = data.wsl.distro || 'Ubuntu';
        badge.textContent = 'Backend: WSL (' + distro + ')';
        badge.className = 'runtime-badge ok';
    }

    function renderWslCard(data) {
        const show = !data.wsl || !data.wsl.available;
        $('#wsl-card').classList.toggle('hidden', !show);
        if (data.settings && data.settings.wslInstallPath) {
            $('#wsl-install-path').value = data.settings.wslInstallPath;
        }
    }

    function renderPrereqs(data) {
        const tools = activeTools(data);
        const items = [];

        if (data.runtime === 'wsl') {
            items.push(
                { label: 'WSL2', ok: data.wsl && data.wsl.available, detail: data.wsl && data.wsl.distro },
                { label: 'Node.js (WSL)', ok: tools.node && tools.node.available, detail: tools.node && tools.node.version },
                { label: 'Git (WSL)', ok: tools.git && tools.git.available, detail: tools.git && tools.git.version },
                { label: 'tippecanoe (WSL)', ok: tools.tippecanoe && tools.tippecanoe.available, detail: tools.tippecanoe && tools.tippecanoe.version },
                { label: 'Map tools (WSL)', ok: tools.mapTools && tools.mapTools.installed, detail: tools.mapTools && tools.mapTools.path }
            );
        } else {
            items.push(
                { label: 'Node.js', ok: tools.node && tools.node.available, detail: tools.node && tools.node.version },
                { label: 'Git', ok: tools.git && tools.git.available, detail: tools.git && tools.git.version },
                { label: 'tippecanoe', ok: tools.tippecanoe && tools.tippecanoe.available, detail: tools.tippecanoe && tools.tippecanoe.version },
                { label: 'Map tools', ok: tools.mapTools && tools.mapTools.installed, detail: tools.mapTools && tools.mapTools.path }
            );
        }

        $('#prereq-list').innerHTML = items.map((item) => {
            const dot = item.ok ? 'ok' : 'bad';
            const extra = item.detail ? ` <span class="muted">(${escapeHtml(String(item.detail))})</span>` : '';
            return `<li><span class="dot ${dot}"></span><span>${escapeHtml(item.label)}${extra}</span></li>`;
        }).join('');
    }

    function renderOutputs(data) {
        const panel = $('#outputs-panel');
        const games = [
            { key: 'ets2', label: 'ETS2', out: data.outputs.ets2 },
            { key: 'ats', label: 'ATS', out: data.outputs.ats }
        ];
        panel.innerHTML = games.map((g) => {
            const gen = g.out.generated ? `Generated ${formatBytes(g.out.sizeBytes)}` : 'Not generated yet';
            const active = g.out.active ? 'Active in TruckDeck' : 'Not active';
            const activateBtn = g.out.generated && !g.out.active
                ? `<button type="button" class="btn ghost" data-activate="${g.key}">Activate</button>`
                : '';
            return `<div class="output-tile">
                <div><strong>${g.label}</strong><br>${gen} · ${active}</div>
                <div class="output-actions">${activateBtn}</div>
            </div>`;
        }).join('');

        panel.querySelectorAll('[data-activate]').forEach((btn) => {
            btn.addEventListener('click', async () => {
                btn.disabled = true;
                try {
                    await api('/activate', { method: 'POST', body: JSON.stringify({ game: btn.dataset.activate }) });
                    await refreshStatus();
                } catch (err) {
                    alert(err.message);
                } finally {
                    btn.disabled = false;
                }
            });
        });
    }

    function escapeHtml(s) {
        return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    function canGenerate(data) {
        if (!data.wsl || !data.wsl.available) return false;
        const tools = data.wslTools || {};
        return tools.mapTools && tools.mapTools.installed &&
            tools.tippecanoe && tools.tippecanoe.available;
    }

    async function refreshStatus() {
        statusData = await api('/status');
        renderRuntimeBadge(statusData);
        renderWslCard(statusData);
        renderPrereqs(statusData);
        renderOutputs(statusData);

        if (statusData.settings) {
            if (statusData.settings.ets2GamePath) $('#ets2-path').value = statusData.settings.ets2GamePath;
            if (statusData.settings.atsGamePath) $('#ats-path').value = statusData.settings.atsGamePath;
            setFieldStatus('ets2', statusData.settings.ets2Valid);
            setFieldStatus('ats', statusData.settings.atsValid);
        }

        const busy = !!(statusData.job && statusData.job.status === 'running');
        $('#btn-setup-tools').disabled = busy || !(statusData.wsl && statusData.wsl.available);
        $('#btn-install-wsl').disabled = busy;
        $('#btn-generate').disabled = busy || !canGenerate(statusData);

        if (statusData.job && statusData.job.status === 'running') {
            showJob(statusData.job);
            activeJobId = statusData.job.id;
            startPolling();
        }
    }

    function showJob(job) {
        $('#job-card').classList.remove('hidden');
        $('#job-progress').style.width = (job.progress || 0) + '%';
        $('#job-message').textContent = (job.progress || 0) + '% — ' + (job.message || 'Working…');
        $('#job-log').textContent = job.logTail || '';
        const logEl = $('#job-log');
        logEl.scrollTop = logEl.scrollHeight;
    }

    function startPolling() {
        if (pollTimer) return;
        pollTimer = setInterval(async () => {
            if (!activeJobId) return;
            try {
                const job = await api('/jobs/' + activeJobId);
                showJob(job);
                if (job.status === 'completed' || job.status === 'failed') {
                    stopPolling();
                    activeJobId = null;
                    if (job.status === 'failed') {
                        $('#job-message').textContent = 'Failed — ' + (job.message || 'see log');
                    }
                    await refreshStatus();
                    if (job.status === 'completed' && pendingGames.length > 1) {
                        pendingGames.shift();
                        await runNextGame();
                    } else {
                        pendingGames = [];
                        $('#btn-generate').disabled = !canGenerate(statusData);
                    }
                }
            } catch (err) {
                console.error(err);
            }
        }, 1500);
    }

    function stopPolling() {
        if (pollTimer) {
            clearInterval(pollTimer);
            pollTimer = null;
        }
    }

    async function runNextGame() {
        if (!pendingGames.length) return;
        const activate = $('#gen-activate').checked;
        const job = await api('/generate', {
            method: 'POST',
            body: JSON.stringify({ games: [pendingGames[0]], activate })
        });
        activeJobId = job.id;
        showJob(job);
        startPolling();
    }

    async function savePaths() {
        const body = {
            ets2GamePath: $('#ets2-path').value.trim(),
            atsGamePath: $('#ats-path').value.trim(),
            wslInstallPath: $('#wsl-install-path').value.trim(),
            mapGenerationBackend: 'wsl'
        };
        const settings = await api('/settings', { method: 'POST', body: JSON.stringify(body) });
        setFieldStatus('ets2', settings.ets2Valid);
        setFieldStatus('ats', settings.atsValid);
    }

    async function browse(game) {
        const result = await api('/browse-path', { method: 'POST', body: JSON.stringify({ game }) });
        if (result.cancelled) return;
        const input = game === 'ats' ? $('#ats-path') : $('#ets2-path');
        input.value = result.path || '';
        setFieldStatus(game, result.valid);
    }

    async function browseWsl() {
        const result = await api('/browse-path', { method: 'POST', body: JSON.stringify({ purpose: 'wsl' }) });
        if (result.cancelled) return;
        $('#wsl-install-path').value = result.path || '';
    }

    async function detect(game) {
        const result = await api('/detect-path', { method: 'POST', body: JSON.stringify({ game }) });
        if (!result.path) {
            setFieldStatus(game, false, 'Steam path not found');
            return;
        }
        const input = game === 'ats' ? $('#ats-path') : $('#ets2-path');
        input.value = result.path;
        setFieldStatus(game, result.valid);
    }

    $('#btn-refresh-status').addEventListener('click', () => refreshStatus().catch(alertError));
    $('#btn-save-paths').addEventListener('click', () => savePaths().catch(alertError));
    $('#btn-browse-wsl').addEventListener('click', () => browseWsl().catch(alertError));

    $('#btn-install-wsl').addEventListener('click', async () => {
        const installPath = $('#wsl-install-path').value.trim();
        if (!installPath) {
            alert('Choose a folder for WSL install (e.g. D:\\WSL).');
            return;
        }
        $('#btn-install-wsl').disabled = true;
        try {
            const job = await api('/install-wsl', {
                method: 'POST',
                body: JSON.stringify({ installPath })
            });
            activeJobId = job.id;
            pendingGames = [];
            showJob(job);
            startPolling();
        } catch (err) {
            alertError(err);
            $('#btn-install-wsl').disabled = false;
        }
    });

    $('#btn-setup-tools').addEventListener('click', async () => {
        $('#btn-setup-tools').disabled = true;
        try {
            const job = await api('/setup-tools', { method: 'POST', body: '{}' });
            activeJobId = job.id;
            pendingGames = [];
            showJob(job);
            startPolling();
        } catch (err) {
            alertError(err);
        }
    });

    $('#btn-generate').addEventListener('click', async () => {
        pendingGames = [];
        if ($('#gen-ets2').checked) pendingGames.push('ets2');
        if ($('#gen-ats').checked) pendingGames.push('ats');
        if (!pendingGames.length) {
            alert('Select at least one game.');
            return;
        }
        $('#btn-generate').disabled = true;
        try {
            await savePaths();
            await runNextGame();
        } catch (err) {
            alertError(err);
            $('#btn-generate').disabled = !canGenerate(statusData);
        }
    });

    document.querySelectorAll('[data-browse]').forEach((btn) => {
        btn.addEventListener('click', () => browse(btn.dataset.browse).catch(alertError));
    });
    document.querySelectorAll('[data-detect]').forEach((btn) => {
        btn.addEventListener('click', () => detect(btn.dataset.detect).catch(alertError));
    });

    function alertError(err) {
        alert(err && err.message ? err.message : String(err));
    }

    refreshStatus().catch(alertError);
})();
