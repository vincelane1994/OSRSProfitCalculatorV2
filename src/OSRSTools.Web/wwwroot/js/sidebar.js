// Sidebar toggle functionality
document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.getElementById('sidebar');
    const toggleBtn = document.getElementById('sidebarToggle');
    const storageKey = 'sidebar-collapsed';

    // Restore state from localStorage
    const isCollapsed = localStorage.getItem(storageKey) === 'true';
    if (isCollapsed) {
        sidebar.classList.add('collapsed');
        document.body.classList.add('sidebar-collapsed');
    }

    // Toggle sidebar
    toggleBtn.addEventListener('click', function () {
        sidebar.classList.toggle('collapsed');
        document.body.classList.toggle('sidebar-collapsed');
        localStorage.setItem(storageKey, sidebar.classList.contains('collapsed'));
    });

    // Highlight active nav link based on current URL
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');

    navLinks.forEach(function (link) {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase())) {
            link.classList.add('active');
        }
    });

    // If on root path, highlight Dashboard
    if (currentPath === '/' || currentPath === '') {
        const dashboardLink = document.querySelector('a[href="/"]') ||
            document.querySelector('a[href="/Home"]') ||
            document.querySelector('a[href="/Home/Index"]');
        if (dashboardLink) {
            dashboardLink.classList.add('active');
        }
    }
});
