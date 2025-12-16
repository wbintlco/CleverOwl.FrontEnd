function formatNumber(input) {
    return input.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

function formatPercent(input) {
    return (parseFloat(input) * 100).toFixed(2) + "%"
}