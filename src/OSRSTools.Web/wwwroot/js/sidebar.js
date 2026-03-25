// Sidebar toggle — overlay on mobile, collapse on desktop
document.addEventListener('DOMContentLoaded', function () {
    const sidebar  = document.getElementById('sidebar');
    const toggleBtn = document.getElementById('sidebarToggle');
    const backdrop  = document.getElementById('sidebarBackdrop');
    const storageKey = 'sidebar-collapsed';

    const isMobile = () => window.innerWidth <= 768;

    // Restore desktop collapsed state
    if (!isMobile() && localStorage.getItem(storageKey) === 'true') {
        sidebar.classList.add('collapsed');
        document.body.classList.add('sidebar-collapsed');
    }

    toggleBtn.addEventListener('click', function () {
        if (isMobile()) {
            sidebar.classList.toggle('mobile-open');
            backdrop.classList.toggle('visible');
        } else {
            sidebar.classList.toggle('collapsed');
            document.body.classList.toggle('sidebar-collapsed');
            localStorage.setItem(storageKey, sidebar.classList.contains('collapsed'));
        }
    });

    // Close sidebar when backdrop is tapped
    backdrop.addEventListener('click', function () {
        sidebar.classList.remove('mobile-open');
        backdrop.classList.remove('visible');
    });

    // Clean up mobile state when resizing to desktop
    window.addEventListener('resize', function () {
        if (!isMobile()) {
            sidebar.classList.remove('mobile-open');
            backdrop.classList.remove('visible');
        }
    });
});
