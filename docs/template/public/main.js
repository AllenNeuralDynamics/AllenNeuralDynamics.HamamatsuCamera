import WorkflowContainer from "./workflow.js"

export default {
    defaultTheme: 'light',
    iconLinks: [{
        icon: 'github',
        href: 'https://TODO',
        title: 'GitHub'
    }],
    start: () => {
        WorkflowContainer.init();
    }
}
