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
});
