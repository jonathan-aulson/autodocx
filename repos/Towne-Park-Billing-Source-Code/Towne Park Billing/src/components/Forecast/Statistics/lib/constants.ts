import React from "react";

export const OVERNIGHT_ADJUSTMENT_RATE = 0.8;

export const HEADER_DISPLAY_NAMES: Record<string, React.ReactNode> = {
    // Occupancy and Rooms
    "occupancy": React.createElement('div', {}, ['Occupancy', React.createElement('br', { key: 'br1' }), '%']),
    "occupied-rooms": React.createElement('div', {}, ['Occupied', React.createElement('br', { key: 'br2' }), 'Rooms']),
    
    // Ratios
    "drive-in-ratio-input": React.createElement('div', {}, ['Drive-In', React.createElement('br', { key: 'br3' }), 'Ratio']),
    "capture-ratio-input": React.createElement('div', {}, ['Capture', React.createElement('br', { key: 'br4' }), 'Ratio']),
    
    // Valet Statistics
    "valet-daily": React.createElement('div', {}, ['Valet Daily', React.createElement('br', { key: 'br5' }), '(Vehicles)']),
    "valet-overnight": React.createElement('div', {}, ['Valet Overnight', React.createElement('br', { key: 'br6' }), '(Vehicles)']),
    "valet-monthly": React.createElement('div', {}, ['Valet Monthly', React.createElement('br', { key: 'br7' }), '(Vehicles)']),
    "valet-comps": React.createElement('div', {}, ['Valet Comps', React.createElement('br', { key: 'br8' }), '(Vehicles)']),
    "valet-aggregator": React.createElement('div', {}, ['Valet Aggregator', React.createElement('br', { key: 'br9' }), '(Vehicles)']),
    
    // Self Statistics
    "self-daily": React.createElement('div', {}, ['Self Daily', React.createElement('br', { key: 'br10' }), '(Vehicles)']),
    "self-overnight": React.createElement('div', {}, ['Self Overnight', React.createElement('br', { key: 'br11' }), '(Vehicles)']),
    "self-monthly": React.createElement('div', {}, ['Self Monthly', React.createElement('br', { key: 'br12' }), '(Vehicles)']),
    "self-comps": React.createElement('div', {}, ['Self Comps', React.createElement('br', { key: 'br13' }), '(Vehicles)']),
    "self-aggregator": React.createElement('div', {}, ['Self Aggregator', React.createElement('br', { key: 'br14' }), '(Vehicles)']),
    
    // Revenue
    "external-revenue": React.createElement('div', {}, ['External', React.createElement('br', { key: 'br15' }), 'Revenue']),
    
    // Legacy entries for backward compatibility
    "type": React.createElement('div', {}, ['Type']),
    "date": React.createElement('div', {}, ['Date']),
    "valet-rate-daily": React.createElement('div', {}, ['Valet Rate', React.createElement('br', { key: 'br16' }), 'Daily']),
    "valet-rate-monthly": React.createElement('div', {}, ['Valet Rate', React.createElement('br', { key: 'br17' }), 'Monthly']),
    "self-rate-daily": React.createElement('div', {}, ['Self Rate', React.createElement('br', { key: 'br18' }), 'Daily']),
    "self-rate-monthly": React.createElement('div', {}, ['Self Rate', React.createElement('br', { key: 'br19' }), 'Monthly']),
    "base-revenue": React.createElement('div', {}, ['Base', React.createElement('br', { key: 'br20' }), 'Revenue'])
}; 