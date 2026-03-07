/**
 * WebDataRocks Helper for Blazor Integration
 * Provides JavaScript interop for pivot grid functionality
 */
window.webDataRocksHelper = {
    pivots: {},

    /**
     * Initialize WebDataRocks instance
     */
    initialize: function (containerId, dotNetHelper) {        // Ensure WebDataRocks library is loaded
        if (typeof WebDataRocks === 'undefined') {            return;
        }

        // Create pivot instance
        this.pivots[containerId] = new WebDataRocks({
            container: `#${containerId}`,
            toolbar: true,
            height: '100%',
            report: {
                dataSource: {
                    data: []
                }
            },
            reportcomplete: function () {                if (dotNetHelper) {
                    const report = JSON.stringify(this.getReport());
                    dotNetHelper.invokeMethodAsync('OnReportCompleteFromJS', report);
                }
            },
            customizeCell: function (cell, data) {
                // Custom styling based on cell value
                if (data.type === 'value' && data.value) {
                    if (data.value < 0) {
                        cell.style.color = '#d9534f'; // Red for negative
                    } else if (data.value > 100) {
                        cell.style['font-weight'] = 'bold';
                    }
                }
            }
        });    },

    /**
     * Load data into pivot grid
     */
    loadData: function (containerId, data) {
        const pivot = this.pivots[containerId];
        if (!pivot) {            return;
        }        pivot.setReport({
            dataSource: {
                data: data
            },
            slice: {
                rows: this.getDefaultRows(data),
                columns: this.getDefaultColumns(data),
                measures: this.getDefaultMeasures(data)
            }
        });
    },

    /**
     * Load complete report configuration
     */
    loadReport: function (containerId, reportConfig) {
        const pivot = this.pivots[containerId];
        if (!pivot) {            return;
        }

        const config = JSON.parse(reportConfig);
        pivot.setReport(config);
    },

    /**
     * Get current report configuration
     */
    getReport: function (containerId) {
        const pivot = this.pivots[containerId];
        if (!pivot) {            return '{}';
        }

        const report = pivot.getReport();
        return JSON.stringify(report);
    },

    /**
     * Export to Excel
     */
    exportToExcel: function (containerId) {
        const pivot = this.pivots[containerId];
        if (!pivot) {            return;
        }

        pivot.exportTo('excel');
    },

    /**
     * Export to PDF
     */
    exportToPdf: function (containerId) {
        const pivot = this.pivots[containerId];
        if (!pivot) {            return;
        }

        pivot.exportTo('pdf');
    },

    /**
     * Refresh the pivot grid
     */
    refresh: function (containerId) {
        const pivot = this.pivots[containerId];
        if (pivot) {
            pivot.refresh();
        }
    },

    /**
     * Auto-detect rows from data
     */
    getDefaultRows: function (data) {
        if (!data || data.length === 0) return [];

        const firstRow = data[0];
        const stringFields = [];

        // Find string fields (good for rows)
        for (const key in firstRow) {
            const value = firstRow[key];
            if (typeof value === 'string' && !key.toLowerCase().includes('id')) {
                stringFields.push({ uniqueName: key });
                if (stringFields.length >= 2) break; // Limit to 2 default rows
            }
        }

        return stringFields;
    },

    /**
     * Auto-detect columns from data
     */
    getDefaultColumns: function (data) {
        if (!data || data.length === 0) return [];

        // For now, return empty - let user configure
        return [];
    },

    /**
     * Auto-detect measures from data
     */
    getDefaultMeasures: function (data) {
        if (!data || data.length === 0) return [{ uniqueName: "Count", aggregation: "count" }];

        const firstRow = data[0];
        const numericFields = [];

        // Find numeric fields (good for aggregation)
        for (const key in firstRow) {
            const value = firstRow[key];
            if (typeof value === 'number' && !key.toLowerCase().includes('id')) {
                numericFields.push({
                    uniqueName: key,
                    aggregation: "sum"
                });
                if (numericFields.length >= 2) break;
            }
        }

        // Always include count
        if (numericFields.length === 0) {
            return [{ uniqueName: "Count", aggregation: "count" }];
        }

        return numericFields;
    }
};