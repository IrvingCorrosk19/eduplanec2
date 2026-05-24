/**
 * Formato visual de nombres (no altera datos enviados en formularios).
 */
(function (global) {
    var LOWER_PARTICLES = { de: 1, del: 1, la: 1, las: 1, los: 1, y: 1, e: 1, da: 1, do: 1, das: 1, dos: 1, van: 1, von: 1, mc: 1, mac: 1 };
    var CODE_LIKE = /^[A-Z0-9]{2,}[-_/][A-Z0-9][A-Z0-9\-_/]*$/;

    function looksLikeEmail(value) {
        return value && value.indexOf('@') >= 0;
    }

    function shouldFormat(value) {
        if (!value) return false;
        var t = String(value).trim();
        if (!t) return false;
        if (looksLikeEmail(t)) return false;
        if (CODE_LIKE.test(t)) return false;
        return true;
    }

    function toTitleCaseWords(text) {
        var lower = text.toLowerCase();
        var titled = lower.replace(/\S+/g, function (word) {
            return word.charAt(0).toUpperCase() + word.slice(1);
        });
        var words = titled.split(/\s+/);
        for (var i = 1; i < words.length; i++) {
            var key = words[i].toLowerCase();
            if (LOWER_PARTICLES[key]) words[i] = key;
        }
        return words.join(' ');
    }

    function format(value) {
        if (!value) return '';
        var t = String(value).trim();
        if (!shouldFormat(t)) return t;
        if (t.indexOf(',') >= 0) {
            var parts = t.split(',').map(function (p) { return p.trim(); }).filter(Boolean);
            if (parts.length === 2)
                return toTitleCaseWords(parts[0]) + ', ' + toTitleCaseWords(parts[1]);
        }
        return toTitleCaseWords(t);
    }

    function applyTo(root) {
        root = root || document;
        var nodes = root.querySelectorAll('[data-person-name], .js-person-name');
        nodes.forEach(function (el) {
            if (el.getAttribute('data-person-name-skip') === 'true') return;
            if (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT') return;
            var raw = el.getAttribute('data-person-name-value');
            var source = raw != null ? raw : el.textContent;
            if (!source) return;
            var formatted = format(source);
            if (raw != null) el.textContent = formatted;
            else el.textContent = formatted;
        });
    }

    global.PersonNameDisplay = {
        format: format,
        shouldFormat: shouldFormat,
        applyTo: applyTo
    };

    function boot() {
        applyTo(document);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }

    if (global.jQuery) {
        global.jQuery(document).on('draw.dt', function () {
            applyTo(document);
        });
    }
})(window);
