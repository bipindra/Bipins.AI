// Terraform input handling
var terraformApp = terraformApp || {};

terraformApp.init = function() {
    $('#analyzeBtn').on('click', function() {
        terraformApp.analyze();
    });
};

terraformApp.getTerraformContent = function() {
    var activeTab = $('.nav-link.active').attr('data-bs-target');
    
    if (activeTab === '#file') {
        var file = $('#terraformFile')[0].files[0];
        if (!file) {
            throw new Error('Please select a file');
        }
        return new Promise(function(resolve, reject) {
            var reader = new FileReader();
            reader.onload = function(e) {
                resolve(e.target.result);
            };
            reader.onerror = reject;
            reader.readAsText(file);
        });
    } else if (activeTab === '#text') {
        var content = $('#terraformText').val();
        if (!content || content.trim().length === 0) {
            throw new Error('Please paste Terraform code');
        }
        return Promise.resolve(content);
    } else if (activeTab === '#url') {
        var url = $('#terraformUrl').val();
        if (!url || url.trim().length === 0) {
            throw new Error('Please enter a URL');
        }
        return terraformApp.fetchFromUrl(url);
    }
    
    throw new Error('Please provide Terraform input');
};

terraformApp.fetchFromUrl = function(url) {
    // For URL input, we'll pass the URL directly to the backend
    return Promise.resolve(url);
};

terraformApp.getSelectedProviders = function() {
    var providers = [];
    if ($('#providerAWS').is(':checked')) providers.push('aws');
    if ($('#providerAzure').is(':checked')) providers.push('azurerm');
    if ($('#providerGCP').is(':checked')) providers.push('google');
    return providers;
};

terraformApp.analyze = function() {
    // Hide previous results and errors
    $('#resultsContainer').addClass('d-none');
    $('#errorAlert').addClass('d-none');
    
    // Show loading
    $('#loadingIndicator').removeClass('d-none');
    $('#analyzeBtn').prop('disabled', true);
    
    // Get Terraform content
    terraformApp.getTerraformContent()
        .then(function(content) {
            var providers = terraformApp.getSelectedProviders();
            var includeOptimizations = $('#includeOptimizations').is(':checked');
            
            // Determine if content is URL or actual content
            var input = {
                cloudProviders: providers,
                options: {
                    timePeriod: 'Monthly',
                    includeOptimizations: includeOptimizations
                }
            };
            
            var activeTab = $('.nav-link.active').attr('data-bs-target');
            if (activeTab === '#url') {
                input.url = content;
            } else {
                input.content = content;
            }
            
            // Call analyze endpoint
            return $.ajax({
                url: '/Home/Analyze',
                method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(input),
                dataType: 'json'
            });
        })
        .then(function(response) {
            if (response && response.analysisId) {
                // Redirect to results page
                window.location.href = '/Analysis/' + response.analysisId;
            } else {
                throw new Error('Invalid response from server');
            }
        })
        .catch(function(error) {
            console.error('Analysis error:', error);
            var errorMsg = 'An error occurred during analysis';
            if (error.responseJSON && error.responseJSON.error) {
                errorMsg = error.responseJSON.error;
            } else if (error.message) {
                errorMsg = error.message;
            }
            $('#errorMessage').text(errorMsg);
            $('#errorAlert').removeClass('d-none');
        })
        .always(function() {
            $('#loadingIndicator').addClass('d-none');
            $('#analyzeBtn').prop('disabled', false);
        });
};
