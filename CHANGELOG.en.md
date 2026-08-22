# Changelog

**English** | [فارسی](CHANGELOG.md)

This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

## 0.1.0-beta.1 — 2026-08-22

First beta — unified ISmsClient abstraction for Iranian SMS providers:
 * Core — Base contracts (ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter), SmsCapabilities enum, and IranSmsException
 * Providers — Kavenegar, Ghasedak, SMS.ir, Melipayamak, and Mock (all supporting Send, BulkSend, OtpSend, and DeliveryStatus)
 * Dependency Injection — Provider-agnostic IranSms.DependencyInjection package, featuring consumer-owned lifecycle management and capability-aware registration via AddIranSms
 * Samples — Basic (standalone console app), AspNetCore (Minimal API), and MultiProvider (capability-based routing)
 * Testing — 150 unit tests featuring FakeTransport implementations for each provider
