const parser = new DOMParser();

const focusElement = (el) => {
    if (!el) return;
    if (!el.hasAttribute('tabindex')) {
        el.setAttribute('tabindex', '-1');
    }
    el.focus({ preventScroll: false });
};

const parseHtml = (html) => parser.parseFromString(html, 'text/html');

const fetchHtmlDocument = async (url, options = {}) => {
    const response = await fetch(url, options);
    const html = await response.text();
    const doc = parseHtml(html);
    return { response, doc };
};

(() => {
    const root = document.querySelector('[data-search-root]');
    if (!root || typeof window.fetch !== 'function' || typeof window.DOMParser !== 'function') {
        return;
    }

    let form,
        resultsContainer,
        spinner,
        loadingContainer,
        statusMessage,
        errorSummary,
        errorList,
        submitButton,
        formWrapper,
        backLink,
        formHeading,
        loadingHeading,
        resultsHeading,
        currentRequest;

    const setRefs = () => {
        form = root.querySelector('[data-search-form]');
        resultsContainer = root.querySelector('[data-search-results]');
        spinner = root.querySelector('[data-search-spinner]');
        loadingContainer = root.querySelector('[data-search-loading]');
        statusMessage = root.querySelector('[data-search-status-message]');
        errorSummary = root.querySelector('[data-search-error]');
        errorList = root.querySelector('[data-search-error-list]');
        submitButton =
            root.querySelector('[data-search-submit]') || root.querySelector('button[type="submit"]');
        formWrapper = root.querySelector('[data-search-form-wrapper]');
        backLink = document.querySelector('.govuk-back-link');
        formHeading = root.querySelector('[data-search-form-heading]');
        loadingHeading = root.querySelector('[data-search-loading-heading]');
        resultsHeading = root.querySelector('[data-search-results-heading]');
    };

    const applyBackLink = () => {
        if (!root || !backLink) return;
        const formHref = root.getAttribute('data-back-link-form') || root.getAttribute('data-back-link-href');
        const resultsHref =
            root.getAttribute('data-back-link-results') || root.getAttribute('data-back-link-href');
        const text = root.getAttribute('data-back-link-text');
        const resultsVisible = resultsContainer && !resultsContainer.hidden;

        const hrefToUse = resultsVisible ? resultsHref || formHref : formHref || resultsHref;
        if (hrefToUse) {
            backLink.setAttribute('href', hrefToUse);
        }

        if (text) {
            backLink.textContent = text;
        }

        if (!backLink.dataset.appBackHandler) {
            backLink.addEventListener('click', (event) => {
                const showingResults = resultsContainer && !resultsContainer.hidden;
                const isLoading = loadingContainer && !loadingContainer.hidden;

                if (!showingResults && !isLoading) return;

                event.preventDefault();

                if (isLoading && currentRequest?.abort) {
                    currentRequest.abort();
                }

                setLoading(false);

                if (resultsContainer) resultsContainer.hidden = true;
                if (formWrapper) formWrapper.hidden = false;
                focusElement(formHeading);
                updateStatus('Search form restored. Previous answers retained.');
                applyBackLink();
                attachHandler();
            });
            backLink.dataset.appBackHandler = 'true';
        }
    };

    const updateStatus = (message) => {
        if (statusMessage) {
            statusMessage.textContent = message || '';
        }
    };

    const setLoading = (isLoading) => {
        if (submitButton) {
            submitButton.disabled = isLoading;
        }

        if (spinner) {
            spinner.hidden = !isLoading;
        }

        if (loadingContainer) {
            loadingContainer.hidden = !isLoading;
        }

        if (formWrapper) {
            if (isLoading) {
                formWrapper.hidden = true;
            } else {
                formWrapper.hidden = resultsContainer && !resultsContainer.hidden;
            }
        }

        if (resultsContainer && isLoading) {
            resultsContainer.hidden = true;
            resultsContainer.innerHTML = '';
        }

        updateStatus(isLoading ? 'Searching...' : '');

        if (root) {
            root.setAttribute('aria-busy', isLoading ? 'true' : 'false');
        }

        if (isLoading && loadingHeading) {
            focusElement(loadingHeading);
        }
    };

    const swapContent = (newDoc) => {
        const newRoot = newDoc.querySelector('[data-search-root]');
        if (!newRoot) return;

        const newFormWrapper = newRoot.querySelector('[data-search-form-wrapper]');
        const newResults = newRoot.querySelector('[data-search-results]');
        const newBackHref = newRoot.getAttribute('data-back-link-href');
        const newBackText = newRoot.getAttribute('data-back-link-text');
        const newBackForm = newRoot.getAttribute('data-back-link-form');
        const newBackResults = newRoot.getAttribute('data-back-link-results');

        if (newBackHref) {
            root.setAttribute('data-back-link-href', newBackHref);
        }

        if (newBackText) {
            root.setAttribute('data-back-link-text', newBackText);
        }

        if (newBackForm) {
            root.setAttribute('data-back-link-form', newBackForm);
        }

        if (newBackResults) {
            root.setAttribute('data-back-link-results', newBackResults);
        }

        if (formWrapper && newFormWrapper) {
            formWrapper.innerHTML = newFormWrapper.innerHTML;
        }

        if (resultsContainer && newResults) {
            resultsContainer.innerHTML = newResults.innerHTML;
            resultsContainer.hidden = newResults.hidden;
        }

        applyBackLink();
    };

    const clearErrors = () => {
        if (errorList) {
            errorList.innerHTML = '';
        }
        if (errorSummary) {
            errorSummary.hidden = true;
        }
    };

    const showError = (message) => {
        const fallbackMessage = message || 'We could not complete the search. Please try again.';
        if (errorList) {
            errorList.innerHTML = '';
            const li = document.createElement('li');
            li.textContent = fallbackMessage;
            errorList.appendChild(li);
        }
        if (errorSummary) {
            errorSummary.hidden = false;
            errorSummary.focus?.();
        }
        updateStatus(fallbackMessage);
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        clearErrors();
        setLoading(true);

        const formData = new FormData(form);
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const actionUrl = form.getAttribute('action') || window.location.pathname;
        const controller = new AbortController();
        currentRequest = controller;

        try {
            const { response, doc } = await fetchHtmlDocument(actionUrl, {
                method: 'POST',
                body: formData,
                headers: {
                    Accept: 'text/html',
                    ...(token ? { RequestVerificationToken: token } : {}),
                },
                signal: controller.signal,
            });

            if (!response.ok) {
                showError('We could not complete the search. Please try again.');
                return;
            }

            swapContent(doc);
            setRefs();
            attachHandler();

            const hasResults = resultsContainer && !resultsContainer.hidden;
            if (hasResults) {
                updateStatus('Search complete. Results loaded.');
                focusElement(resultsHeading);
            }
        } catch (error) {
            if (controller.signal.aborted) {
                // Cancelled by user; do not show error
                updateStatus('Search cancelled. Form restored.');
                attachHandler();
                return;
            }
            console.error('Search failed', error);
            showError('We could not complete the search. Please try again.');
        } finally {
            setLoading(false);
            currentRequest = null;
            attachHandler();
        }
    };

    const attachHandler = () => {
        if (form) {
            form.removeEventListener('submit', handleSubmit);
            form.addEventListener('submit', handleSubmit, { once: true });
        }
    };

    setRefs();
    applyBackLink();
    attachHandler();
})();

(async () => {
    const recordRoot = document.querySelector('[data-record-root]');
    if (!recordRoot || typeof window.fetch !== 'function' || typeof window.DOMParser !== 'function') {
        return;
    }

    const recordUrl = recordRoot.getAttribute('data-record-url');
    const spinner = recordRoot.querySelector('.app-search__loading');
    const content = recordRoot.querySelector('[data-record-content]');

    const focusHeading = () => {
        const heading = recordRoot.querySelector('h1');
        if (heading) {
            focusElement(heading);
        }
    };

    const renderFromDoc = (doc) => {
        const newRoot = doc.querySelector('[data-record-root]');
        const newContent = newRoot?.querySelector('[data-record-content]');

        if (newContent && content) {
            content.replaceWith(newContent);
        } else if (content) {
            content.hidden = false;
        } else if (newContent) {
            recordRoot.appendChild(newContent);
        }

        if (spinner) {
            spinner.hidden = true;
        }

        const newTitle = doc.querySelector('title')?.textContent;
        if (newTitle) {
            document.title = newTitle;
        }

        focusHeading();
    };

    const fetchContent = async () => {
        if (!recordUrl) return;
        try {
            const { doc } = await fetchHtmlDocument(recordUrl, { headers: { Accept: 'text/html' } });
            renderFromDoc(doc);
        } catch (error) {
            console.error('Failed to load record', error);
            if (spinner) {
                const label = spinner.querySelector('.app-spinner__label');
                if (label) {
                    label.textContent = 'We could not load this record. Please try again.';
                }
            }
        }
    };

    if (!content) {
        await fetchContent();
    } else {
        focusHeading();
    }
})();
