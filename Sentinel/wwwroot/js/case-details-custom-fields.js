// Toggle custom field category sections
function toggleCategory(categoryId) {
    const fieldsDiv = document.getElementById(categoryId);
    const chevron = document.getElementById('chevron-' + categoryId);
    
    if (fieldsDiv && chevron) {
        fieldsDiv.classList.toggle('collapsed');
        chevron.classList.toggle('collapsed');
    }
}
