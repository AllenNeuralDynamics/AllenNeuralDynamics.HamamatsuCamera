import WorkflowContainer from "./workflow.js"

export default {
    defaultTheme: 'light',
    iconLinks: [{
        icon: 'github',
        href: 'https://github.com/AllenNeuralDynamics/AllenNeuralDynamics.HamamatsuCamera',
        title: 'GitHub'
    }],
    start: () => {
        WorkflowContainer.init();
    }
}
