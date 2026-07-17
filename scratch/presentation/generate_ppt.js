const pptxgen = require("pptxgenjs");
const fs = require('fs');

const pptx = new pptxgen();
pptx.layout = 'LAYOUT_16x9';

// Define master slide
pptx.defineSlideMaster({
  title: "MASTER_SLIDE",
  background: { color: "1A1A2E" }, // Dark mode background
  objects: [
    { rect: { x: 0, y: 0, w: "100%", h: 0.75, fill: { color: "0F3460" } } }, // Neon blue/purple accent
    { text: { text: "Shatbly - Servers Booking Platform", options: { x: 0.5, y: 0.1, w: "90%", h: 0.5, color: "E94560", fontSize: 18, bold: true, fontFace: "Courier New" } } },
  ]
});

// Helper function
function createSlide(title, bullets, notes) {
    let slide = pptx.addSlide({ masterName: "MASTER_SLIDE" });
    slide.addText(title, { x: 0.5, y: 1, w: "90%", h: 1, fontSize: 36, bold: true, color: "E94560", fontFace: "Courier New" });
    
    let bulletOptions = { x: 0.5, y: 2.2, w: "90%", h: 3.5, fontSize: 24, color: "FFFFFF", bullet: true, lineSpacing: 45, fontFace: "Arial" };
    slide.addText(bullets.map(b => ({ text: b })), bulletOptions);
    if(notes) slide.addNotes(notes);
}

// Slide 1: Title Slide
let slide1 = pptx.addSlide();
slide1.background = { color: "1A1A2E" };
slide1.addText("Shatbly", { x: 0.5, y: 2, w: "90%", h: 1, color: "E94560", fontSize: 60, bold: true, align: "center", fontFace: "Courier New" });
slide1.addText("The Ultimate Servers Booking Platform\nEngineered for Scale. Built for Trust.", { x: 0.5, y: 3.5, w: "90%", h: 1.5, color: "FFFFFF", fontSize: 28, align: "center", fontFace: "Arial" });
slide1.addNotes("Welcome to the presentation of our graduation project: Shatbly. Shatbly is not just a booking application; it is a highly secure, real-time home services ecosystem. Today, we will take you under the hood to explore the advanced architectural paradigms, micro-service domains, and zero-trust security measures that make Shatbly a truly enterprise-ready platform.");

// Slide 2
createSlide(
    "Global Architectural Paradigms",
    [
        "Modular Monolith Architecture",
        "Data Layer: Unit of Work & Generic Repository Patterns",
        "Performance: 100% Asynchronous I/O (async/await)",
        "Security: ID Obfuscation via Hashids"
    ],
    "Shatbly is built on a robust Modular Monolith architecture using ASP.NET Core and Entity Framework Core. To ensure absolute data integrity, we strictly enforce the Unit of Work and Generic Repository patterns—meaning atomic, fail-safe transactions across the board. Furthermore, we implemented a massive security feature: ID Obfuscation using Hashids. The client never sees raw database IDs, entirely eliminating IDOR vulnerabilities and competitor data scraping."
);

// Slide 3
createSlide(
    "The Real-Time Engine (SignalR)",
    [
        "ChatHub: Bidirectional messaging with Connection-Level Authorization.",
        "TrackingHub: Live GPS telemetry (Lat/Lng) for En-Route workers.",
        "NotificationHub: Instant, refresh-free UI updates."
    ],
    "In the gig economy, real-time data is everything. We engineered three distinct SignalR Hubs. Our TrackingHub streams live GPS coordinates from the worker's Flutter app directly to the customer. Our ChatHub provides instant messaging, secured by strict Connection-Level Authorization—users can only join a chat group if the database cryptographically verifies their link to that specific booking."
);

// Slide 4
createSlide(
    "Financial & Wallet Engine",
    [
        "Stripe Integration: PCI-compliant checkout sessions.",
        "Webhook Mapping: Secure, asynchronous payment validation.",
        "Virtual Wallets: Internal ledgers for debits and credits.",
        "Withdrawal System: Automated commission logic & payout requests."
    ],
    "Handling finances requires zero margin for error. We integrated Stripe for PCI-compliant payments, utilizing extensive metadata to map asynchronous webhooks safely back to our database. Inside Shatbly, a double-entry virtual ledger tracks every cent, automatically calculating platform commissions and managing worker withdrawal requests for seamless bank payouts."
);

// Slide 5
createSlide(
    "Background Processing & AI Integration",
    [
        "Hangfire: Resilient, SQL-backed asynchronous task queue.",
        "Groq AI (LLM): High-speed, automated customer support triage.",
        "QuestPDF: Dynamic, localized, high-resolution invoice generation."
    ],
    "To guarantee a lightning-fast UI, heavy lifting is handled in the background. We use SQL-backed Hangfire to process tasks like SMTP email dispatching and automated booking cancellations, ensuring no job is lost during server restarts. We also integrated Groq AI for instant, intelligent customer support, and QuestPDF to generate beautiful, localized receipts on the fly."
);

// Slide 6
createSlide(
    "The Super Admin Experience",
    [
        "Total Oversight: Global app parameters & commission control.",
        "Vetting & Security: ID & CV validation for Worker activation.",
        "Analytics Hub: Complex SQL aggregations for revenue tracking.",
        "Dispute Mediation: Ultimate authority over chat logs and refunds."
    ],
    "The system is divided into three core pillars. First is the Super Admin. This dashboard is the command center. Admins have total oversight to vet worker IDs and CVs before activation, mediate disputes with full access to chat logs, and utilize our Report Controller to run complex SQL aggregations tracking platform revenue and growth."
);

// Slide 7
createSlide(
    "The Worker & Customer Ecosystem",
    [
        "Worker: Schedule Autonomy, Unavailability Blocks, Secure Earnings, Portfolio.",
        "Customer: Service Discovery, Promo Codes, Wallet Top-ups, Rating Algorithm."
    ],
    "The next two pillars are the Worker and the Customer. Workers enjoy complete autonomy—managing recurring availability, blackout dates, and uploading portfolios through our strict File Service. Customers experience a frictionless booking funnel, where they can apply promotional coupons, pay via their virtual wallet, and leave reviews that directly influence our search algorithm."
);

// Slide 8
createSlide(
    "Deep Security & Hardening",
    [
        "Zero-Trust Architecture",
        "Anti-CSRF & XSS Mitigation: Global token validation & HTML encoding.",
        "Strict File I/O: Whitelists, size limits, and Magic Number inspection.",
        "Invisible Endpoints: Admin-only health UI monitoring."
    ],
    "We approached Shatbly with a Zero-Trust mindset. Beyond ID obfuscation, every state-changing action is protected by Anti-CSRF tokens, and all dynamic data is strictly sanitized against XSS. Our File Service doesn't just check extensions; it inspects file Magic Numbers to prevent executable payloads. Finally, our system health dashboards are locked behind strict Admin-only policies, invisible to the public internet."
);

// Slide 9
let slide9 = pptx.addSlide({ masterName: "MASTER_SLIDE" });
slide9.addText("Conclusion & Q&A", { x: 0.5, y: 1, w: "90%", h: 1, fontSize: 40, bold: true, color: "E94560", fontFace: "Courier New", align: "center" });
slide9.addText("Shatbly: The Blueprint for Modern Home Services.\n\nThank You.\n\nQ&A", { x: 0.5, y: 3, w: "90%", h: 2, fontSize: 32, color: "FFFFFF", align: "center", fontFace: "Arial" });
slide9.addNotes("Shatbly is a testament to modern software engineering—a scalable, secure, and feature-rich platform ready for production deployment. We are incredibly proud of the clean architecture, the real-time capabilities, and the rigorous security we have implemented. Thank you for your time, and we would be happy to answer any questions you might have.");

const outputPath = 'C:\\Users\\ahmd5\\source\\repos\\Servers-Booking-Platform\\Shatbly_Presentation.pptx';

pptx.writeFile({ fileName: outputPath }).then(fileName => {
    console.log(`Created presentation: ${fileName}`);
});
