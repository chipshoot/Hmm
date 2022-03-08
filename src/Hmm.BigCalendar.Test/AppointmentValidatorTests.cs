using Hmm.BigCalendar.DomainEntity;
using Hmm.BigCalendar.Validators;
using Hmm.Utility.Misc;
using Hmm.Utility.Validation;
using Moq;
using System;
using Xunit;

namespace Hmm.BigCalendar.Test;

public class AppointmentValidatorTests : IDisposable
{
    private readonly DateTime _currentDate = new(2022, 1, 3);
    private readonly IHmmValidator<Appointment> _validator;
    private ProcessingResult _processResult;

    public AppointmentValidatorTests()
    {
        var fakeDate = new Mock<IDateTimeProvider>();
        fakeDate.Setup(d => d.UtcNow).Returns(_currentDate);
        _validator = new DefaultAppointmentValidator(fakeDate.Object);
        _processResult = new ProcessingResult();
    }

    [Theory]
    [InlineData("2022-4-2", "2022-3-20", false)]
    [InlineData("2022-4-2", "2022-4-3", true)]
    public void Appointment_EndDate_Earlier_Than_StartDate(DateTime start, DateTime end, bool expectedSuccess)
    {
        // Arrange
        var appointment = new Appointment
        {
            HostId = Guid.NewGuid(),
            StartTime = start,
            EndTime = end,
            Contact = new ContactInfo()
        };

        // Act
        var result = _validator.IsValidEntity(appointment, _processResult);

        // Assert
        if (expectedSuccess)
        {
            Assert.True(result);
            Assert.False(_processResult.HasError);
        }
        else
        {
            Assert.False(result);
            Assert.True(_processResult.HasError);
        }
    }

    [Theory]
    [InlineData("2022-1-2", false)]
    [InlineData("2022-3-2", true)]
    public void Appointment_StartDate_Earlier_Than_Today(DateTime start, bool expectedSuccess)
    {
        // Arrange
        var appointment = new Appointment
        {
            HostId = Guid.NewGuid(),
            StartTime = start,
            EndTime = start.AddDays(1),
            Contact = new ContactInfo()
        };

        // Act
        var result = _validator.IsValidEntity(appointment, _processResult);

        // Assert
        if (expectedSuccess)
        {
            Assert.True(result);
            Assert.False(_processResult.HasError);
        }
        else
        {
            Assert.False(result);
            Assert.True(_processResult.HasError);
        }
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", false)]
    [InlineData("D9F50D8C-41AA-4F05-B7FF-653370B8C895", true)]
    public void Appointment_HostId_Invalid(Guid hostId, bool expectedSuccess)
    {
        // Arrange
        var appointment = new Appointment
        {
            HostId = hostId,
            StartTime = _currentDate,
            EndTime = _currentDate.AddDays(1),
            Contact = new ContactInfo()
        };

        // Act
        var result = _validator.IsValidEntity(appointment, _processResult);

        // Assert
        if (expectedSuccess)
        {
            Assert.True(result);
            Assert.False(_processResult.HasError);
        }
        else
        {
            Assert.False(result);
            Assert.True(_processResult.HasError);
        }
    }

    [Fact]
    public void Appointment_Contact_Invalid()
    {
        // Arrange
        var appointment = new Appointment
        {
            HostId = Guid.NewGuid(),
            StartTime = _currentDate,
            EndTime = _currentDate.AddDays(1),
            Contact = null
        };

        // Act
        var result = _validator.IsValidEntity(appointment, _processResult);

        // Assert
        Assert.False(result);
        Assert.True(_processResult.HasError);
    }

    public void Dispose()
    {
        _processResult = new ProcessingResult();
        GC.SuppressFinalize(this);
    }
}