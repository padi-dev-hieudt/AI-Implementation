/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/auth.js
   Authentication state manager and global UI wiring.

   Public API:
     Auth.ready  — Promise resolved once Auth.init() completes.
                   Page scripts await this before accessing Auth.user.
     Auth.user   — Current UserProfileDto, or null for guests.

   Depends on: ApiService, UserService, Utils
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── Auth ────────────────────────────────────────────────────────
   Manages authentication state and the login / register forms.
   ─────────────────────────────────────────────────────────────── */
const Auth = (() => {
    let _user = null;

    /* Auth.ready resolves after the first Auth.init() call completes */
    let _resolveReady;
    const _ready = new Promise(resolve => { _resolveReady = resolve; });

    async function init() {
        try {
            const res = await UserService.me();
            _user = res.data;
        } catch {
            _user = null;
        }
        _updateNav();
        _resolveReady();   // unblock all page scripts awaiting Auth.ready
    }

    function _updateNav() {
        const guestNav = document.getElementById('guestNav');
        const userNav  = document.getElementById('userNav');
        if (!guestNav || !userNav) return;

        if (_user) {
            guestNav.classList.add('d-none');
            userNav.classList.remove('d-none');

            const initial = (_user.username || '?')[0].toUpperCase();
            const bg      = Utils.strToColor(_user.username || '');

            const navAvatar   = document.getElementById('navAvatar');
            const navUsername = document.getElementById('navUsername');
            const ddUsername  = document.getElementById('ddUsername');
            const ddEmail     = document.getElementById('ddEmail');
            const ddRole      = document.getElementById('ddRole');

            if (navAvatar) {
                navAvatar.textContent      = initial;
                navAvatar.style.background = bg;
            }
            if (navUsername) navUsername.textContent = _user.username;
            if (ddUsername)  ddUsername.textContent  = _user.username;
            if (ddEmail)     ddEmail.textContent     = _user.email;
            if (ddRole) {
                ddRole.textContent = _user.role;
                ddRole.className   = `badge mt-1 badge-role-${(_user.role || 'user').toLowerCase()}`;
            }

            const ddProfileLink = document.getElementById('ddProfileLink');
            if (ddProfileLink && _user.id) ddProfileLink.href = `/profile/${_user.id}`;

            // Show "Quản trị" link only for Admin users
            const ddAdminLink = document.getElementById('ddAdminLink');
            if (ddAdminLink) ddAdminLink.classList.toggle('d-none', _user.role !== 'Admin');
        } else {
            guestNav.classList.remove('d-none');
            userNav.classList.add('d-none');
        }

        /* Intercept "New post" button for guests */
        const btnNew = document.getElementById('btnNewPost');
        if (btnNew) {
            btnNew.addEventListener('click', e => {
                if (!_user) {
                    e.preventDefault();
                    const modal = document.getElementById('loginModal');
                    modal && bootstrap.Modal.getOrCreateInstance(modal).show();
                }
            });
        }
    }

    function setupLoginForm() {
        const form = document.getElementById('loginForm');
        if (!form) return;

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const email    = document.getElementById('loginEmail')?.value.trim();
            const password = document.getElementById('loginPassword')?.value;
            const errEl    = document.getElementById('loginError');
            const spinner  = document.getElementById('loginSpinner');
            const btn      = document.getElementById('btnLogin');
            if (!email || !password) return;

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const res = await UserService.login({ email, password });
                _user = res.data;
                _updateNav();
                bootstrap.Modal.getOrCreateInstance(document.getElementById('loginModal'))?.hide();
                Utils.flash(`Chào mừng trở lại, ${_user.username}!`, 'success');
                form.reset();
                setTimeout(() => location.reload(), 800);
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function setupRegisterForm() {
        const form = document.getElementById('registerForm');
        if (!form) return;

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const username        = document.getElementById('regUsername')?.value.trim();
            const email           = document.getElementById('regEmail')?.value.trim();
            const password        = document.getElementById('regPassword')?.value;
            const confirmPassword = document.getElementById('regConfirmPassword')?.value;
            const errEl           = document.getElementById('registerError');
            const spinner         = document.getElementById('registerSpinner');
            const btn             = document.getElementById('btnRegister');
            if (!username || !email || !password || !confirmPassword) return;

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const res = await UserService.register({ username, email, password, confirmPassword });
                _user = res.data;
                _updateNav();
                bootstrap.Modal.getOrCreateInstance(document.getElementById('registerModal'))?.hide();
                Utils.flash(`Đăng ký thành công! Chào mừng ${_user.username}!`, 'success');
                form.reset();
                setTimeout(() => location.reload(), 800);
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function setupLogout() {
        const btn = document.getElementById('btnLogout');
        if (!btn) return;
        btn.addEventListener('click', async e => {
            e.preventDefault();
            try {
                await UserService.logout();
                _user = null;
                Utils.flash('Đã đăng xuất.', 'info');
                setTimeout(() => { location.href = '/'; }, 600);
            } catch (err) {
                Utils.flash('Đăng xuất thất bại: ' + err.message, 'danger');
            }
        });
    }

    function setupPasswordToggles() {
        document.querySelectorAll('.toggle-pwd-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const targetId = btn.dataset.target;
                const el       = document.getElementById(targetId);
                if (!el) return;
                const show     = el.type === 'password';
                el.type        = show ? 'text' : 'password';
                const icon     = btn.querySelector('i');
                if (icon) icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
            });
        });
    }

    function setupSearch() {
        function doSearch(input) {
            const q = (input?.value || '').trim().toLowerCase();
            document.querySelectorAll('.topic-row').forEach(row => {
                const title = row.querySelector('.topic-title')?.textContent.toLowerCase() || '';
                const meta  = row.querySelector('.topic-meta')?.textContent.toLowerCase()  || '';
                row.style.display = (!q || title.includes(q) || meta.includes(q)) ? '' : 'none';
            });
        }

        const btn   = document.getElementById('searchBtn');
        const input = document.getElementById('searchInput');
        if (btn && input) {
            btn.addEventListener('click', () => doSearch(input));
            input.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(input); });
        }

        const btnM   = document.getElementById('searchBtnMobile');
        const inputM = document.getElementById('searchInputMobile');
        if (btnM && inputM) {
            btnM.addEventListener('click', () => doSearch(inputM));
            inputM.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(inputM); });
        }
    }

    return {
        get ready() { return _ready; },
        get user()  { return _user; },
        init,
        setupLoginForm,
        setupRegisterForm,
        setupLogout,
        setupPasswordToggles,
        setupSearch
    };
})();

/* ── Global DOMContentLoaded ─────────────────────────────────────
   Wires auth modals, nav, search, and resolves Auth.ready so that
   page scripts (loaded via @section Scripts) can safely proceed.
   ─────────────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', async () => {
    Auth.setupPasswordToggles();
    Auth.setupLoginForm();
    Auth.setupRegisterForm();
    Auth.setupLogout();

    /* "Go back" button on the Error view */
    document.getElementById('btnGoBack')?.addEventListener('click', e => {
        e.preventDefault();
        history.back();
    });

    await Auth.init();   // resolves Auth.ready internally via _resolveReady()
});
