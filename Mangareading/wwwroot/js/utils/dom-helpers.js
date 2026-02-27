/**
 * DOM Helpers Utility
 * Provides DOM manipulation helpers for the Manga Reader application
 */

class DOMHelpers {
    /**
     * Create an element with attributes and content
     * param {string} tag - Tag name
     * param {Object} attributes - Element attributes
     * param {string|HTMLElement|Array} content - Element content
     * @returns {HTMLElement} - Created element
     */
    static createElement(tag, attributes = {}, content = null) {
        const element = document.createElement(tag);
        
        // Set attributes
        Object.entries(attributes).forEach(([key, value]) => {
            if (key === 'className') {
                element.className = value;
            } else if (key === 'style' && typeof value === 'object') {
                Object.entries(value).forEach(([prop, val]) => {
                    element.style[prop] = val;
                });
            } else {
                element.setAttribute(key, value);
            }
        });
        
        // Set content
        if (content !== null) {
            if (typeof content === 'string') {
                element.textContent = content;
            } else if (content instanceof HTMLElement) {
                element.appendChild(content);
            } else if (Array.isArray(content)) {
                content.forEach(item => {
                    if (typeof item === 'string') {
                        element.appendChild(document.createTextNode(item));
                    } else if (item instanceof HTMLElement) {
                        element.appendChild(item);
                    }
                });
            }
        }
        
        return element;
    }
    
    /**
     * Find the closest element matching a selector
     * param {HTMLElement} element - Starting element
     * param {string} selector - CSS selector
     * @returns {HTMLElement|null} - Matching element or null
     */
    static closest(element, selector) {
        if (element.closest) {
            return element.closest(selector);
        }
        
        // Polyfill for older browsers
        let current = element;
        
        while (current) {
            if (current.matches && current.matches(selector)) {
                return current;
            }
            current = current.parentElement;
        }
        
        return null;
    }
    
    /**
     * Add event listener with delegation
     * param {HTMLElement|Document} element - Parent element
     * param {string} eventType - Event type
     * param {string} selector - CSS selector for delegation
     * param {Function} handler - Event handler
     */
    static addDelegatedEventListener(element, eventType, selector, handler) {
        element.addEventListener(eventType, function(e) {
            const target = DOMHelpers.closest(e.target, selector);
            
            if (target) {
                handler.call(target, e);
            }
        });
    }
    
    /**
     * Toggle element visibility
     * param {HTMLElement|string} element - Element or selector
     * param {boolean} show - Whether to show the element
     */
    static toggleVisibility(element, show) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        if (show === undefined) {
            element.style.display = element.style.display === 'none' ? '' : 'none';
        } else {
            element.style.display = show ? '' : 'none';
        }
    }
    
    /**
     * Add or remove a class based on condition
     * param {HTMLElement|string} element - Element or selector
     * param {string} className - Class to toggle
     * param {boolean} condition - Whether to add the class
     */
    static toggleClass(element, className, condition) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        if (condition === undefined) {
            element.classList.toggle(className);
        } else {
            if (condition) {
                element.classList.add(className);
            } else {
                element.classList.remove(className);
            }
        }
    }
    
    /**
     * Set multiple CSS properties
     * param {HTMLElement|string} element - Element or selector
     * param {Object} styles - CSS properties
     */
    static setStyles(element, styles) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        Object.entries(styles).forEach(([property, value]) => {
            element.style[property] = value;
        });
    }
    
    /**
     * Get or set element data attribute
     * param {HTMLElement|string} element - Element or selector
     * param {string} key - Data attribute name
     * param {*} value - Value to set (if undefined, returns current value)
     * @returns {string|null} - Current value if getting
     */
    static data(element, key, value) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return null;
        
        const dataKey = 'data-' + key.replace(/([A-Z])/g, '-$1').toLowerCase();
        
        if (value === undefined) {
            return element.getAttribute(dataKey);
        } else {
            element.setAttribute(dataKey, value);
        }
    }
    
    /**
     * Animate an element
     * param {HTMLElement|string} element - Element or selector
     * param {string} animationName - Animation name
     * param {number} duration - Animation duration in ms
     * param {Function} callback - Callback after animation
     */
    static animate(element, animationName, duration = 300, callback) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        element.style.animation = `${animationName} ${duration}ms`;
        
        const handleAnimationEnd = () => {
            element.style.animation = '';
            element.removeEventListener('animationend', handleAnimationEnd);
            
            if (typeof callback === 'function') {
                callback();
            }
        };
        
        element.addEventListener('animationend', handleAnimationEnd);
    }
    
    /**
     * Fade in an element
     * param {HTMLElement|string} element - Element or selector
     * param {number} duration - Animation duration in ms
     * param {Function} callback - Callback after animation
     */
    static fadeIn(element, duration = 300, callback) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        element.style.opacity = '0';
        element.style.display = '';
        
        setTimeout(() => {
            element.style.transition = `opacity ${duration}ms`;
            element.style.opacity = '1';
            
            setTimeout(() => {
                element.style.transition = '';
                
                if (typeof callback === 'function') {
                    callback();
                }
            }, duration);
        }, 10);
    }
    
    /**
     * Fade out an element
     * param {HTMLElement|string} element - Element or selector
     * param {number} duration - Animation duration in ms
     * param {Function} callback - Callback after animation
     */
    static fadeOut(element, duration = 300, callback) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        
        if (!element) return;
        
        element.style.transition = `opacity ${duration}ms`;
        element.style.opacity = '0';
        
        setTimeout(() => {
            element.style.display = 'none';
            element.style.transition = '';
            
            if (typeof callback === 'function') {
                callback();
            }
        }, duration);
    }
}

// Export the DOMHelpers class
window.DOMHelpers = DOMHelpers;
