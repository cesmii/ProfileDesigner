import React from 'react'
import { SVGIcon } from './SVGIcon'

class Fab extends React.Component {

    render() {
        var iconName = this.props.iconName;
        var opacity = this.props.opacity;
        var bgColor = this.props.bgColor+opacity;
        var iconColor = this.props.color;
        var size = this.props.size;
        var fabStyle = {
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: iconColor,
            backgroundColor: bgColor,
            width: size,
            height: size
        };

        return (
            <div className="fab" style={fabStyle}>
                <SVGIcon name={iconName} size="24" fill={iconColor} />
                {/* <MaterialIcon icon={iconName} /> */}
            </div>
        );

    }
}


export default Fab;