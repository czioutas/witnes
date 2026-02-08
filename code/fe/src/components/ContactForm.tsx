import { useState } from "react";
import { Button } from "./ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "./ui/card";
import { Mail, User } from "lucide-react";

export function ContactForm() {
  const [fullName, setFullName] = useState("");
  const [message, setMessage] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (!fullName.trim() || !message.trim()) {
      alert("Please fill in both fields before sending.");
      return;
    }

    const subject = encodeURIComponent(`Support Request from ${fullName}`);
    const body = encodeURIComponent(`Hello Witnes Team,

${message}

Best regards,
${fullName}`);

    const mailtoUrl = `mailto:hello@witnes.io?subject=${subject}&body=${body}`;

    // Try to open email client
    try {
      window.location.href = mailtoUrl;

      // Show a fallback message after a short delay
      setTimeout(() => {
        if (
          confirm(
            "If your email client didn't open automatically, would you like to copy the email details to your clipboard?",
          )
        ) {
          const emailText = `To: hello@witnes.io
Subject: Support Request from ${fullName}

Hello Witnes Team,

${message}

Best regards,
${fullName}`;

          navigator.clipboard
            .writeText(emailText)
            .then(() => {
              alert(
                "Email details copied to clipboard! Please paste into your email client.",
              );
            })
            .catch(() => {
              alert(`Please manually copy this to your email client:

To: hello@witnes.io
Subject: Support Request from ${fullName}

Hello Witnes Team,

${message}

Best regards,
${fullName}`);
            });
        }
      }, 2000);
    } catch (error) {
      console.error("Failed to open email client:", error);
      alert(
        "Unable to open email client automatically. Please send your message to hello@witnes.io",
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Mail className="h-5 w-5" />
          Contact Support
        </CardTitle>
        <CardDescription>
          Fill out the form below and we'll open your email client to send us a
          message.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form className="space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-2">
            <label
              htmlFor="fullName"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Full Name *
            </label>
            <div className="relative">
              <User className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
              <input
                id="fullName"
                type="text"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                className="flex h-10 w-full rounded-md border border-input bg-background pl-10 pr-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
                placeholder="Your full name"
              />
            </div>
          </div>

          <div className="space-y-2">
            <label
              htmlFor="message"
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
            >
              Message *
            </label>
            <textarea
              id="message"
              required
              rows={6}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              placeholder="Tell us how we can help you..."
            />
          </div>

          <Button type="submit" className="w-full">
            <Mail className="mr-2 h-4 w-4" />
            Send Message
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}
