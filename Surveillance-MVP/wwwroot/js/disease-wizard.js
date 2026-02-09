// Disease Creation Wizard JavaScript
let currentStep = 1;
const totalSteps = 5;

document.addEventListener('DOMContentLoaded', function () {
    // Show initial step
    showStep(currentStep);

    // Next button
    document.getElementById('nextBtn').addEventListener('click', function () {
        if (validateStep(currentStep)) {
            if (currentStep < totalSteps) {
                currentStep++;
                showStep(currentStep);
            }
        }
    });

    // Previous button
    document.getElementById('prevBtn').addEventListener('click', function () {
        if (currentStep > 1) {
            currentStep--;
            showStep(currentStep);
        }
    });

    // Child diseases toggle
    const createChildrenToggle = document.getElementById('createChildrenToggle');
    const childDiseasesSection = document.getElementById('childDiseasesSection');
    const noChildrenMessage = document.getElementById('noChildrenMessage');

    createChildrenToggle.addEventListener('change', function () {
        if (this.checked) {
            childDiseasesSection.style.display = 'block';
            noChildrenMessage.style.display = 'none';
        } else {
            childDiseasesSection.style.display = 'none';
            noChildrenMessage.style.display = 'block';
        }
    });

    // Symptom checkboxes
    document.getElementById('selectAllSymptomsWizard').addEventListener('change', function () {
        const checkboxes = document.querySelectorAll('.symptom-checkbox-wizard');
        checkboxes.forEach(cb => {
            cb.checked = this.checked;
            toggleSymptomDetails(cb);
        });
    });

    document.querySelectorAll('.symptom-checkbox-wizard').forEach(checkbox => {
        checkbox.addEventListener('change', function () {
            toggleSymptomDetails(this);
        });
    });

    // Select common symptoms button
    document.getElementById('selectCommonSymptoms')?.addEventListener('click', function () {
        // In a real implementation, you'd mark certain symptoms as "common" in the database
        // For now, just select the first 5 as an example
        const checkboxes = document.querySelectorAll('.symptom-checkbox-wizard');
        checkboxes.forEach((cb, index) => {
            if (index < 5) {
                cb.checked = true;
                toggleSymptomDetails(cb);
            }
        });
    });

    // Deselect all symptoms button
    document.getElementById('deselectAll')?.addEventListener('click', function () {
        const checkboxes = document.querySelectorAll('.symptom-checkbox-wizard');
        checkboxes.forEach(cb => {
            cb.checked = false;
            toggleSymptomDetails(cb);
        });
        document.getElementById('selectAllSymptomsWizard').checked = false;
    });
});

function showStep(step) {
    // Hide all steps
    const steps = document.querySelectorAll('.step-content');
    steps.forEach(s => s.classList.remove('active'));

    // Show current step
    document.getElementById('step' + step).classList.add('active');

    // Update wizard step indicators
    document.querySelectorAll('.wizard-step').forEach((stepEl, index) => {
        const stepNum = index + 1;
        if (stepNum < step) {
            stepEl.classList.remove('active');
            stepEl.classList.add('completed');
        } else if (stepNum === step) {
            stepEl.classList.add('active');
            stepEl.classList.remove('completed');
        } else {
            stepEl.classList.remove('active', 'completed');
        }
    });

    // Update buttons
    const prevBtn = document.getElementById('prevBtn');
    const nextBtn = document.getElementById('nextBtn');
    const submitBtn = document.getElementById('submitBtn');

    if (step === 1) {
        prevBtn.style.display = 'none';
    } else {
        prevBtn.style.display = 'inline-block';
    }

    if (step === totalSteps) {
        nextBtn.style.display = 'none';
        submitBtn.style.display = 'inline-block';
        populateReview();
    } else {
        nextBtn.style.display = 'inline-block';
        submitBtn.style.display = 'none';
    }
}

function validateStep(step) {
    if (step === 1) {
        // Validate basic information
        const name = document.querySelector('[name="Disease.Name"]').value.trim();
        const code = document.querySelector('[name="Disease.Code"]').value.trim();

        if (!name || !code) {
            alert('Please fill in required fields (Name and Code)');
            return false;
        }
    }
    return true;
}

function toggleSymptomDetails(checkbox) {
    const row = checkbox.closest('tr');
    const details = row.querySelectorAll('.symptom-detail-wizard');
    details.forEach(detail => {
        detail.disabled = !checkbox.checked;
    });
}

function populateReview() {
    // Basic information
    document.getElementById('review-name').textContent = 
        document.querySelector('[name="Disease.Name"]').value || '-';
    
    document.getElementById('review-code').textContent = 
        document.querySelector('[name="Disease.Code"]').value || '-';
    
    const categorySelect = document.querySelector('[name="Disease.DiseaseCategoryId"]');
    document.getElementById('review-category').textContent = 
        categorySelect.options[categorySelect.selectedIndex]?.text || 'None';
    
    const notifiable = document.querySelector('[name="Disease.IsNotifiable"]').checked;
    document.getElementById('review-notifiable').textContent = notifiable ? 'Yes' : 'No';

    // Child diseases
    const createChildren = document.getElementById('createChildrenToggle').checked;
    const childCard = document.getElementById('review-children-card');
    
    if (createChildren) {
        childCard.style.display = 'block';
        const childText = document.querySelector('[name="ChildDiseaseList"]').value;
        const lines = childText.split('\n').filter(l => l.trim());
        
        if (lines.length > 0) {
            const list = '<ul class="mb-0">' + 
                lines.map(l => '<li>' + l.split('|')[0] + '</li>').join('') + 
                '</ul>';
            document.getElementById('review-children-list').innerHTML = 
                '<strong>' + lines.length + ' child disease(s):</strong>' + list;
        } else {
            document.getElementById('review-children-list').textContent = 'None specified';
        }
    } else {
        childCard.style.display = 'none';
    }

    // Symptoms
    const selectedSymptoms = [];
    document.querySelectorAll('.symptom-checkbox-wizard:checked').forEach(cb => {
        const row = cb.closest('tr');
        const symptomName = row.querySelector('strong').textContent;
        const common = row.querySelector('[name^="symptom_common_"]').value === 'true' ? ' (Common)' : '';
        selectedSymptoms.push(symptomName + common);
    });

    const symptomsCard = document.getElementById('review-symptoms-card');
    if (selectedSymptoms.length > 0) {
        symptomsCard.style.display = 'block';
        const list = '<ul class="mb-0">' + 
            selectedSymptoms.map(s => '<li>' + s + '</li>').join('') + 
            '</ul>';
        document.getElementById('review-symptoms-list').innerHTML = 
            '<strong>' + selectedSymptoms.length + ' symptom(s):</strong>' + list;
    } else {
        symptomsCard.style.display = 'none';
    }

    // Custom fields
    const selectedFields = [];
    document.querySelectorAll('[name^="field_"]:checked').forEach(cb => {
        const label = cb.parentElement.querySelector('strong').textContent;
        selectedFields.push(label);
    });

    const fieldsCard = document.getElementById('review-fields-card');
    if (selectedFields.length > 0) {
        fieldsCard.style.display = 'block';
        const list = '<ul class="mb-0">' + 
            selectedFields.map(f => '<li>' + f + '</li>').join('') + 
            '</ul>';
        document.getElementById('review-fields-list').innerHTML = 
            '<strong>' + selectedFields.length + ' custom field(s):</strong>' + list;
    } else {
        fieldsCard.style.display = 'none';
    }
}
