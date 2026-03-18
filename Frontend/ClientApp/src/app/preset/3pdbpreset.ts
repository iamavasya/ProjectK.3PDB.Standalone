import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const ThreePDBPreset = definePreset(Aura, {
    
    // Setup custom theme for 3PDB
    semantic: {
        colorScheme: {
            light: {
            },
            dark: {
            }
        }
    }
    
});

export default ThreePDBPreset;