(function () {
    const selectedRecordText = document.getElementById("selectedRecordText");
    const fetchStatus = document.getElementById("fetchStatus");

    const progressWrap = document.getElementById("fetchProgressWrap");
    const progressBar = document.getElementById("fetchProgressBar");
    const progressText = document.getElementById("fetchProgressText");

    const fetchContent = document.getElementById("fetchContent");
    const contactsContainer = document.getElementById("contactsContainer");
    const externalContainer = document.getElementById("externalContainer");
    const dataContainer = document.getElementById("dataContainer");

    function setStatus(text) {
        if (fetchStatus) {
            fetchStatus.textContent = text || "";
        }
    }

    function setSelectedText(text) {
        if (selectedRecordText) {
            selectedRecordText.textContent = text || "No record selected yet.";
        }
    }

    function setProgressVisible(visible) {
        if (progressWrap) {
            progressWrap.style.display = visible ? "" : "none";
        }
    }

    function setContentVisible(visible) {
        if (fetchContent) {
            fetchContent.style.display = visible ? "" : "none";
        }
    }

    function setProgress(percent, label) {
        const pct = Math.max(0, Math.min(100, percent));

        if (progressBar) {
            progressBar.style.width = pct + "%";
            progressBar.setAttribute("aria-valuenow", String(pct));
        }

        if (progressText) {
            progressText.textContent = label || "";
        }
    }

    function showFetchTab() {
        const tabButton = document.getElementById("tab-third-tab");
        if (!tabButton || !window.bootstrap) {
            return;
        }

        const tab = bootstrap.Tab.getOrCreateInstance(tabButton);
        tab.show();
    }

    function escapeHtml(s) {
        return (s || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#039;");
    }

    function renderContacts(contacts) {
        if (!contactsContainer) {
            return;
        }

        if (!contacts || contacts.length === 0) {
            contactsContainer.innerHTML = "<div class=\"text-muted\">No contacts provided.</div>";
            return;
        }

        const cards = contacts.map(c => {
            const name = escapeHtml(c.name || "");
            const role = escapeHtml(c.role || "");
            const tel = escapeHtml(c.telephone || "");
            const email = escapeHtml(c.email || "");
            const address = escapeHtml(c.address || "");

            const lines = [];
            if (role) lines.push("<div class=\"text-muted\">" + role + "</div>");
            if (tel) lines.push("<div>Telephone: " + tel + "</div>");
            if (email) lines.push("<div>Email: " + email + "</div>");
            if (address) lines.push("<div>Address: " + address + "</div>");

            return "<div class=\"border rounded p-2 mb-2\">" +
                "<div class=\"fw-semibold\">" + name + "</div>" +
                lines.join("") +
                "</div>";
        });

        contactsContainer.innerHTML = cards.join("");
    }

    function renderExternal(external) {
        if (!externalContainer) {
            return;
        }

        if (!external) {
            externalContainer.innerHTML = "None provided.";
            externalContainer.className = "mb-0 text-muted";
            return;
        }

        const name = escapeHtml(external.systemName || "");
        const url = escapeHtml(external.url || "");
        const notes = escapeHtml(external.notes || "");

        externalContainer.className = "mb-0";
        externalContainer.innerHTML =
            "<div class=\"fw-semibold mb-1\">" + name + "</div>" +
            "<div class=\"mb-1\"><a href=\"" + url + "\" target=\"_blank\" rel=\"noopener noreferrer\">" + url + "</a></div>" +
            "<div class=\"text-muted\">" + notes + "</div>";
    }

    function renderData(sections) {
        if (!dataContainer) {
            return;
        }

        if (!sections || sections.length === 0) {
            dataContainer.innerHTML = "No record data provided.";
            dataContainer.className = "mb-0 text-muted";
            return;
        }

        const html = sections.map(s => {
            const title = escapeHtml(s.title || "");
            const fields = (s.fields || []).map(f => {
                const label = escapeHtml(f.label || "");
                const value = escapeHtml(f.value || "");
                return "<tr><th class=\"text-nowrap\" style=\"width: 220px;\">" + label + "</th><td>" + value + "</td></tr>";
            }).join("");

            return "<div class=\"mb-3\">" +
                "<div class=\"fw-semibold mb-2\">" + title + "</div>" +
                "<div class=\"table-responsive\"><table class=\"table table-sm mb-0\"><tbody>" + fields + "</tbody></table></div>" +
                "</div>";
        }).join("");

        dataContainer.className = "mb-0";
        dataContainer.innerHTML = html;
    }

    async function fetchRecord(url) {
        setContentVisible(false);
        setProgressVisible(true);
        setProgress(10, "Contacting custodian…");
        setStatus("");

        const startedAt = Date.now();

        try {
            const resp = await fetch(url, { method: "GET" });
            if (!resp.ok) {
                setProgressVisible(false);
                setStatus("Failed to retrieve record.");
                return;
            }

            setProgress(60, "Retrieving record…");

            const data = await resp.json();

            const elapsedMs = Date.now() - startedAt;
            const remainingMs = Math.max(0, 700 - elapsedMs);
            if (remainingMs > 0) {
                await new Promise(r => setTimeout(r, remainingMs));
            }

            setProgress(100, "Complete");

            const header = (data.custodianName || "Custodian") + " — " + (data.recordType || "Record") + " — NHS " + (data.nhsNumber || "");
            setSelectedText(header);

            renderContacts(data.contacts || []);
            renderExternal(data.externalSystem || null);
            renderData(data.dataSections || []);

            setProgressVisible(false);
            setContentVisible(true);

            if (data.summary) {
                setStatus(data.summary);
            }
        } catch (e) {
            setProgressVisible(false);
            setStatus("Failed to retrieve record.");
        }
    }

    document.addEventListener("click", function (e) {
        const target = e.target;
        if (!target) {
            return;
        }

        const link = target.closest(".fetch-record-link");
        if (!link) {
            return;
        }

        e.preventDefault();

        const url = link.getAttribute("href") || "";
        if (!url) {
            return;
        }

        showFetchTab();
        setSelectedText("Selected record: " + url);
        fetchRecord(url);
    });

    function init() {
        setSelectedText("No record selected yet.");
        setProgressVisible(false);
        setContentVisible(false);
    }

    init();
})();
