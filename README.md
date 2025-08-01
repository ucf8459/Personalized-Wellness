# 🏥 Wellness Platform - Comprehensive Health Dashboard

A .NET-based proof of concept dashboard for a comprehensive wellness and longevity platform. This dashboard demonstrates the integration of biomarker data, validated assessments (PROMIS), treatment recommendations with evidence classification, and longitudinal tracking capabilities.

## 🎯 **Project Overview**

This platform integrates multiple health data streams to provide personalized wellness insights and evidence-based treatment recommendations. It serves as a proof of concept for validating the user experience of combining biomarker data, PROMIS assessments, and treatment tracking.

### **Key Features**
- 📊 **Biomarker Analysis** - Track and visualize health markers over time
- 📋 **PROMIS Assessments** - Validated patient-reported outcome measures
- 💊 **Treatment Database** - Evidence-based treatment recommendations
- 📈 **Longitudinal Tracking** - Monitor health trends and treatment effectiveness
- 🔬 **Evidence Classification** - Oxford CEBM levels for treatment recommendations
- 🛡️ **HIPAA-Ready** - Built with healthcare privacy in mind

## 🚀 **Quick Start**

### **Prerequisites**
- .NET 9.0 SDK
- SQLite (included)
- Modern web browser

### **Installation**

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/wellness-platform.git
   cd wellness-platform
   ```

2. **Run the application**
   ```bash
   dotnet run
   ```

3. **Access the dashboard**
   - Open your browser to `http://localhost:5272`
   - Login with demo credentials:
     - **Email**: `demo@wellness.com`
     - **Password**: `Demo123!`

## 📊 **Demo Data**

The application comes pre-loaded with sample data including:
- **Biomarker Results**: Cardiovascular, metabolic, and hormonal markers
- **PROMIS Assessments**: Physical function, fatigue, pain interference scores
- **Treatment Database**: Evidence-based interventions with safety ratings
- **User Treatments**: Sample treatment history and effectiveness tracking

## 🏗️ **Architecture**

### **Technology Stack**
- **Backend**: .NET 9.0 with ASP.NET Core Blazor Server
- **Database**: SQLite with Entity Framework Core
- **Frontend**: Bootstrap 5 with custom healthcare styling
- **Authentication**: ASP.NET Core Identity
- **Charts**: Chart.js (planned integration)

### **Database Schema**
- **HealthProfiles**: Core user health data
- **BiomarkerResults**: Lab test results with reference ranges
- **PromisResults**: Patient-reported outcome measures
- **Treatments**: Evidence-based treatment database
- **UserTreatments**: Individual treatment tracking
- **LifestyleMetrics**: Daily health metrics

## 📋 **Development Status**

See [TODO.md](./TODO.md) for detailed development progress and roadmap.

### **Current Progress**
- ✅ **Core Infrastructure** - Database, models, authentication
- ✅ **Basic Dashboard** - Health overview and navigation
- ✅ **Biomarker Tracking** - Data visualization and analysis
- 🔄 **Treatment Engine** - Recommendation system in development
- ⏳ **Chart Integration** - Chart.js implementation planned

## 🎯 **Learning Objectives**

This proof of concept validates:
- **User Experience**: Combining multiple health data streams
- **Evidence Classification**: Effectiveness of evidence-based treatment decisions
- **Correlation Analysis**: Biomarker changes with assessment improvements
- **Treatment Database**: Practical implementation of comprehensive treatment data

## 🔬 **Evidence-Based Approach**

### **Treatment Classification**
- **Level 1**: Systematic reviews and meta-analyses
- **Level 2**: Randomized controlled trials
- **Level 3**: Cohort studies and observational data
- **Level 4**: Case reports and limited human data
- **Level 5**: Preclinical and animal studies

### **Safety Ratings**
- **5/5**: Very safe, minimal side effects
- **4/5**: Generally safe, mild side effects
- **3/5**: Moderate safety profile
- **2/5**: Requires medical supervision
- **1/5**: High risk, experimental

## 🛡️ **Privacy & Security**

- **HIPAA Compliance**: Built with healthcare privacy standards
- **Data Encryption**: End-to-end encryption for health data
- **Access Controls**: Role-based authentication
- **Audit Logging**: Comprehensive access tracking

## 📚 **Documentation**

- [Project Specification](./wellness_dashboard_spec.md) - Detailed technical requirements
- [Development TODO](./TODO.md) - Current progress and roadmap
- [API Documentation](./docs/api.md) - Coming soon
- [User Guide](./docs/user-guide.md) - Coming soon

## 🤝 **Contributing**

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### **Development Guidelines**
- Follow .NET coding conventions
- Add unit tests for new features
- Update documentation for API changes
- Ensure HIPAA compliance for health data features

## 📄 **License**

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🆘 **Support**

- **Issues**: [GitHub Issues](https://github.com/yourusername/wellness-platform/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/wellness-platform/discussions)
- **Documentation**: [Project Wiki](https://github.com/yourusername/wellness-platform/wiki)

## 🙏 **Acknowledgments**

- **PROMIS**: Patient-Reported Outcomes Measurement Information System
- **Oxford CEBM**: Oxford Centre for Evidence-Based Medicine
- **Chart.js**: Charting library for data visualization
- **Bootstrap**: Frontend framework for responsive design

---

**⚠️ Disclaimer**: This is a proof of concept and should not be used for actual medical decisions. Always consult with healthcare professionals for medical advice. 